using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Nintroller
{
    internal static class Logging
    {
        private static readonly Dictionary<int, string> _win32Errors = new Dictionary<int, string>();

        [Conditional("DEBUG")]
        public static void LogWin32Error(string message)
            => LogWin32Error(message, Marshal.GetLastWin32Error());

        [Conditional("DEBUG")]
        public static void LogWin32Error(string message, WIN32_ERROR result)
            => LogWin32Error(message, (int)result);

        [Conditional("DEBUG")]
        public static void LogWin32Error(string message, int result)
        {
            Debug.Write(message);
            Debug.WriteLine(GetWin32ErrorMessage(result));
        }

        [Conditional("DEBUG")]
        public static void SanityCheckResult(bool success, WIN32_ERROR result)
        {
            if (!success && result != WIN32_ERROR.ERROR_SUCCESS)
                return;

            if (!success)
                Debug.WriteLine("Result was failure but GetLastWin32Error returned success");
            else if (result != WIN32_ERROR.ERROR_SUCCESS)
                Logging.LogWin32Error("Result was success but GetLastWin32Error returned an error", result);
        }

        private static string GetWin32ErrorMessage(int result)
        {
            if (!_win32Errors.TryGetValue(result, out string message))
            {
                message = $": {new Win32Exception(result).Message} ({result})";
                _win32Errors.Add(result, message);
            }

            return message;
        }
    }
}