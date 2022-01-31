using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public sealed class LinuxI2CBusAdapter : II2CBusAdapter
    {
        const int I2CSlave = 0x0703;
        const int OpenReadWrite = 0x2;

        readonly object _accessLock = new();
        readonly string _filename;
        readonly ILogger _logger;

        int _handle;

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

        public void Read(int deviceAddress, Span<byte> buffer)
        {
            lock (_accessLock)
            {
                SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);

                var readBuffer = new byte[buffer.Length];
                SafeNativeMethods.Read(_handle, readBuffer, readBuffer.Length, 0);

                readBuffer.CopyTo(buffer);
            }
        }

        public void Write(int deviceAddress, ReadOnlySpan<byte> buffer)
        {
            lock (_accessLock)
            {
                var writeBuffer = buffer.ToArray();

                var ioCtlResult = SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
                var writeResult = SafeNativeMethods.Write(_handle, writeBuffer, writeBuffer.Length, 0);

                _logger.Log(LogLevel.Debug, "Written on '{0}' (Device address = {1}; Buffer = {2}; IOCTL result = {3}; Write result = {4}; Error = {5}).", _filename, deviceAddress,
                    buffer.ToHexString(), ioCtlResult, writeResult, Marshal.GetLastWin32Error());
            }
        }

        public void WriteRead(int deviceAddress, ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            // TODO: Consider keeping instance of the arrays and work with them inside the lock.
            var writeBufferArray = writeBuffer.ToArray();
            var readBufferArray = new byte[readBuffer.Length];

            lock (_accessLock)
            {
                SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
                SafeNativeMethods.Write(_handle, writeBufferArray, writeBufferArray.Length, 0);
                SafeNativeMethods.Read(_handle, readBufferArray, readBufferArray.Length, 0);

                readBufferArray.CopyTo(readBuffer);
            }
        }
    }
}