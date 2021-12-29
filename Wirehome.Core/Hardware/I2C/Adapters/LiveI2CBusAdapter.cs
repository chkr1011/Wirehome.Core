//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Device.I2c;
//using Wirehome.Core.Extensions;

//namespace Wirehome.Core.Hardware.I2C.Adapters
//{
//    public class LiveI2CBusAdapter : II2CBusAdapter
//    {
//        readonly Dictionary<int, I2cDevice> _devices = new Dictionary<int, I2cDevice>();
//        readonly ILogger _logger;

//        public LiveI2CBusAdapter(ILogger logger)
//        {
//            _logger = logger;
//        }

//        public void Read(int deviceAddress, Span<byte> buffer)
//        {
//            lock (_devices)
//            {
//                var device = PrepareDevice(deviceAddress);
//                device.Read(buffer);
//            }
//        }

//        public void Write(int deviceAddress, ReadOnlySpan<byte> buffer)
//        {
//            lock (_devices)
//            {
//                var device = PrepareDevice(deviceAddress);
//                device.Write(buffer);
//            }

//            _logger.LogDebug($"Written on I2C bus 1 (address: {deviceAddress}, data: {buffer.ToHexString()}).");
//        }

//        public void WriteRead(int deviceAddress, ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
//        {
//            lock (_devices)
//            {
//                var device = PrepareDevice(deviceAddress);
//                device.WriteRead(writeBuffer, readBuffer);
//            }
//        }

//        I2cDevice PrepareDevice(int deviceAddress)
//        {
//            if (!_devices.TryGetValue(deviceAddress, out var device))
//            {
//                _logger.LogInformation($"Opening I2C device {deviceAddress}.");

//                device = I2cDevice.Create(new I2cConnectionSettings(1, deviceAddress));
//                _devices[deviceAddress] = device;
//            }

//            return device;
//        }
//    }
//}

