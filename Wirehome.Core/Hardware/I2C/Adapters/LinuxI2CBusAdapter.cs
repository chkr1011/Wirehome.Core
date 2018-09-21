using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Extensions;

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

        public LinuxI2CBusAdapter(int busId, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<LinuxI2CBusAdapter>();

            _filename = "/dev/i2c-" + busId;
        }

        public void Enable()
        {
            lock (_accessLock)
            {
                _handle = NativeOpen(_filename, OpenReadWrite);
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
                var ioCtlResult = NativeIoctl(_handle, I2CSlave, deviceAddress);
                var writeResult = NativeWrite(_handle, buffer.Array, buffer.Count, buffer.Offset);
                
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
                NativeIoctl(_handle, I2CSlave, deviceAddress);
                NativeRead(_handle, buffer.Array, buffer.Count, buffer.Offset);
            }
        }

        public void WriteRead(int deviceAddress, ArraySegment<byte> writeBuffer, ArraySegment<byte> readBuffer)
        {
            lock (_accessLock)
            {
                NativeIoctl(_handle, I2CSlave, deviceAddress);
                NativeWrite(_handle, writeBuffer.Array, writeBuffer.Count, writeBuffer.Offset);
                NativeRead(_handle, readBuffer.Array, readBuffer.Count, readBuffer.Offset);
            }
        }

        [DllImport("libc.so.6", EntryPoint = "open", SetLastError = true)]
        private static extern int NativeOpen(string fileName, int mode);

        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int NativeIoctl(int fd, int request, int data);

        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        private static extern int NativeRead(int handle, byte[] data, int length, int offset);

        [DllImport("libc.so.6", EntryPoint = "write", SetLastError = true)]
        private static extern int NativeWrite(int handle, byte[] data, int length, int offset);
    }
}