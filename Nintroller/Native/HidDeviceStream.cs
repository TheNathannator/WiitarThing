using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace Nintroller
{
    using static PInvoke;
    using static WIN32_ERROR;
    using static FILE_SHARE_MODE;
    using static FILE_CREATION_DISPOSITION;
    using static FILE_FLAGS_AND_ATTRIBUTES;

    public class HidDeviceStream : IDisposable
    {
        private readonly string _path;
        private SafeFileHandle _handle;

        private readonly EventWaitHandle _readWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly EventWaitHandle _writeWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public int InputLength { get; private set; }
        public int OutputLength { get; private set; }

        public bool UseHidD { get; set; }

        public HidDeviceStream(string path)
        {
            _path = path;
        }

        public void Dispose()
        {
            _handle?.Close();
            _handle = null;
        }

        public bool Open(bool exclusive)
        {
            if (_handle != null && !_handle.IsInvalid)
                return true;

            _handle = CreateFile(
                _path,
                (uint) (FILE_SHARE_READ | FILE_SHARE_WRITE),
                exclusive ? FILE_SHARE_NONE : (FILE_SHARE_READ | FILE_SHARE_WRITE),
                null,
                OPEN_EXISTING,
                FILE_FLAG_OVERLAPPED,
                null
            );

            if (_handle == null || _handle.IsInvalid)
            {
                Logging.LogWin32Error("Failed to open HID device");
                return false;
            }

            if (!HidD_GetPreparsedData(_handle, out var hidData) || hidData.IsNull)
            {
                Logging.LogWin32Error("Could not get HID preparsed data");
                return false;
            }

            var status = HidP_GetCaps(hidData, out var caps);
            if (status < 0) // HRESULT, not Win32 error
            {
                Logging.LogWin32Error("Could not get HID capabilities", status);
                return false;
            }

            InputLength = caps.InputReportByteLength;
            OutputLength = caps.OutputReportByteLength;

            return true;
        }

        private void CheckDisposed()
        {
            if (_handle == null || _handle.IsInvalid)
                throw new ObjectDisposedException(nameof(_handle));
        }

        public unsafe bool Read(Span<byte> buffer)
        {
            CheckDisposed();

            if (buffer.Length < InputLength)
                return false;

            var overlapped = new NativeOverlapped()
            {
                EventHandle = _readWaitHandle.SafeWaitHandle.DangerousGetHandle()
            };

            bool success;
            uint bytesRead;
            if (UseHidD)
            {
                fixed (byte* ptr = buffer)
                    success = HidD_GetInputReport(_handle, ptr, (uint)buffer.Length);
                bytesRead = (uint)InputLength;
            }
            else
            {
                success = ReadFile(_handle, buffer, &bytesRead, &overlapped);
            }

            WIN32_ERROR result = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (!success && result == ERROR_IO_PENDING)
            {
                _readWaitHandle.WaitOne();
                success = GetOverlappedResult(_handle, in overlapped, out bytesRead, true);
                result = (WIN32_ERROR)Marshal.GetLastWin32Error();
            }

            if (!success && result != ERROR_SUCCESS)
            {
                Logging.LogWin32Error("Device read failed", result);
                return false;
            }

            Logging.SanityCheckResult(success, result);

            return bytesRead == InputLength;
        }

        public unsafe bool Write(Span<byte> buffer)
        {
            CheckDisposed();

            if (buffer.Length > OutputLength)
                return false;

            Span<byte> writeBuffer = stackalloc byte[OutputLength];
            buffer.CopyTo(writeBuffer);

            var overlapped = new NativeOverlapped
            {
                EventHandle = _writeWaitHandle.SafeWaitHandle.DangerousGetHandle()
            };

            bool success;
            if (UseHidD)
            {
                fixed (byte* ptr = buffer)
                    success = HidD_SetOutputReport(_handle, ptr, (uint)writeBuffer.Length);
            }
            else
            {
                success = WriteFile(_handle, writeBuffer, null, &overlapped);
            }

            WIN32_ERROR result = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (!success && result == ERROR_IO_PENDING)
            {
                _readWaitHandle.WaitOne();
                success = GetOverlappedResult(_handle, in overlapped, out _, true);
                result = (WIN32_ERROR)Marshal.GetLastWin32Error();
            }

            if (!success && result != ERROR_SUCCESS)
            {
                Logging.LogWin32Error("Device write failed", result);
                return false;
            }

            Logging.SanityCheckResult(success, result);

            return true;
        }
    }
}