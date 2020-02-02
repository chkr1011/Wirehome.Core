using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using Wirehome.Core.Extensions;
using Wirehome.Core.Interop;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public class LinuxI2CBusAdapter : II2CBusAdapter
    {
        private const int I2CSlave = 0x0703;
        private const int OpenReadWrite = 0x2;

        private readonly object _accessLock = new object();
        private readonly ILogger _logger;
        private readonly string _filename;

        private int _handle;

        public LinuxI2CBusAdapter(int busId, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _filename = "/dev/i2c-" + busId;
        }

        public void Enable()
        {
            lock (_accessLock)
            {
                _handle = SafeNativeMethods.Open(_filename, OpenReadWrite);
            }

            if (_handle != 0)
            {
                _logger.Log(LogLevel.Trace, $"Opened '{_filename}' (Handle = {_handle}).");
            }
            else
            {
                _logger.Log(LogLevel.Error, $"Error while opening '{_filename}'.");
            }
        }

        public void Write(int deviceAddress, ArraySegment<byte> buffer)
        {
            lock (_accessLock)
            {
                var ioCtlResult = SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
                var writeResult = SafeNativeMethods.Write(_handle, buffer.Array, buffer.Count, buffer.Offset);

                _logger.Log(
                    LogLevel.Debug,
                    "Written on '{0}' (Device address = {1}; Buffer = {2}; IOCTL result = {3}; Write result = {4}; Error = {5}).",
                    _filename,
                    deviceAddress,
                    buffer.ToHexString(),
                    ioCtlResult,
                    writeResult,
                    Marshal.GetLastWin32Error());
            }
        }

        public void Read(int deviceAddress, ArraySegment<byte> buffer)
        {
            lock (_accessLock)
            {
                SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
                SafeNativeMethods.Read(_handle, buffer.Array, buffer.Count, buffer.Offset);
            }
        }

        public void WriteRead(int deviceAddress, ArraySegment<byte> writeBuffer, ArraySegment<byte> readBuffer)
        {
            lock (_accessLock)
            {
                SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
                SafeNativeMethods.Write(_handle, writeBuffer.Array, writeBuffer.Count, writeBuffer.Offset);
                SafeNativeMethods.Read(_handle, readBuffer.Array, readBuffer.Count, readBuffer.Offset);
            }
        }
    }
}