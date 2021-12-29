using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public class TestI2CBusAdapter : II2CBusAdapter
    {
        readonly ILogger _logger;

        public TestI2CBusAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Read(int deviceAddress, Span<byte> buffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake Read: Device = {deviceAddress}");
        }

        public void Write(int deviceAddress, ReadOnlySpan<byte> buffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake Write: Device = {deviceAddress}; Buffer = {buffer.ToHexString()}");
        }

        public void WriteRead(int deviceAddress, ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake WriteRead: Device = {deviceAddress}; WriteBuffer = {writeBuffer.ToHexString()}");
        }
    }
}