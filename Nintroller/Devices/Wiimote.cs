using System;
using System.Collections.Generic;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nintroller
{
    public class Wiimote
    {
        public static event Action<Wiimote> Added;
        public static event Action<Wiimote> Removed;

        private const ushort WIIMOTE_VID = 0x057e;
        private const ushort WIIMOTE_PID = 0x0306;

        private static readonly DeviceNotificationListener _listener = new DeviceNotificationListener();
        private static readonly Dictionary<string, Wiimote> _devices = new Dictionary<string, Wiimote>();

        static Wiimote()
        {
            _listener.DeviceArrived += (args) => OnDeviceAdded(args.SymLink);
            _listener.DeviceRemoved += (args) => OnDeviceRemoved(args.SymLink, remove: true);
            _listener.StartListen(DeviceInterfaceIds.HidDevice);
        }

        private static void OnDeviceAdded(string path)
        {
            if (_devices.ContainsKey(path))
                return;

            if (!HidDeviceStream.GetHardwareIds(path, out ushort vendorId, out ushort productId) ||
                vendorId != WIIMOTE_VID || productId != WIIMOTE_PID)
                return;

            var device = new Wiimote(path);
            _devices[path] = device;
            Added?.Invoke(device);
        }

        private static void OnDeviceRemoved(string path, bool remove)
        {
            if (!_devices.TryGetValue(path, out var device))
                return;

            Removed?.Invoke(device);
            device.Dispose();

            if (remove)
                _devices.Remove(path);
        }

        public static void RefreshDevices()
        {
            _listener.StopListen(DeviceInterfaceIds.HidDevice);

            foreach (string path in _devices.Keys)
                OnDeviceRemoved(path, remove: false);

            _devices.Clear();

            for (int i = 0; Devcon.FindByInterfaceGuid(DeviceInterfaceIds.HidDevice, out string path, out _, i); i++)
                OnDeviceAdded(path);

            _listener.StartListen(DeviceInterfaceIds.HidDevice);
        }

        private HidDeviceStream _stream;

        private Wiimote(string path)
        {
            _stream = new HidDeviceStream(path);
        }

        public bool Open(bool exclusive)
        {
            return _stream.Open(exclusive);
        }

        private void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}