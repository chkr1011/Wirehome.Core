using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public class TestI2CBusAdapter : II2CBusAdapter
    {
        private readonly ILogger _logger;

        public TestI2CBusAdapter(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<TestI2CBusAdapter>();
        }

        public void Write(int deviceAddress, ArraySegment<byte> buffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake Write: Device = {deviceAddress}; Buffer = {buffer.ToHexString()}");
        }

        public void Read(int deviceAddress, ArraySegment<byte> buffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake Read: Device = {deviceAddress}");
        }

        public void WriteRead(int deviceAddress, ArraySegment<byte> writeBuffer, ArraySegment<byte> readBuffer)
        {
            //_logger.Log(LogLevel.Information, $"Fake WriteRead: Device = {deviceAddress}; WriteBuffer = {writeBuffer.ToHexString()}");
        }
    }
}
