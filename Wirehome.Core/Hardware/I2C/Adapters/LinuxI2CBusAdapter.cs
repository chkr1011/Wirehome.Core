using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Hardware.I2C.Adapters;

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

    public int Read(int deviceAddress, byte[] buffer)
    {
        lock (_accessLock)
        {
            SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
            return SafeNativeMethods.Read(_handle, buffer, buffer.Length, 0);
        }
    }

    public int Write(int deviceAddress, byte[] buffer)
    {
        lock (_accessLock)
        {
            SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);
            var writeResult = SafeNativeMethods.Write(_handle, buffer, buffer.Length, 0);

            _logger.LogDebug("Written to '{0}' (Address: {1}; Buffer: {2}; Result: {3}).", _filename, deviceAddress, buffer.ToHexString(), writeResult);

            return writeResult;
        }
    }

    public int WriteRead(int deviceAddress, byte[] writeBuffer, byte[] readBuffer)
    {
        lock (_accessLock)
        {
            SafeNativeMethods.Ioctl(_handle, I2CSlave, deviceAddress);

            var writeResult = SafeNativeMethods.Write(_handle, writeBuffer, writeBuffer.Length, 0);
            if (writeResult < 0)
            {
                // Cancel the read because the write was already failing.
                return writeResult;
            }

            return SafeNativeMethods.Read(_handle, readBuffer, readBuffer.Length, 0);
        }
    }
}