using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Hardware.I2C.Adapters;

public sealed class TestI2CBusAdapter : II2CBusAdapter
{
    readonly ILogger _logger;

    public TestI2CBusAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Read(int deviceAddress, byte[] buffer)
    {
        //_logger.Log(LogLevel.Information, $"Fake Read: Device = {deviceAddress}");
        return 0;
    }

    public int Write(int deviceAddress, byte[] buffer)
    {
        //_logger.Log(LogLevel.Information, $"Fake Write: Device = {deviceAddress}; Buffer = {buffer.ToHexString()}");
        return 0;
    }

    public int WriteRead(int deviceAddress, byte[] writeBuffer, byte[] readBuffer)
    {
        //_logger.Log(LogLevel.Information, $"Fake WriteRead: Device = {deviceAddress}; WriteBuffer = {writeBuffer.ToHexString()}");
        return 0;
    }
}