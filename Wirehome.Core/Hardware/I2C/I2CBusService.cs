using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Wirehome.Core.Contracts;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Hardware.I2C.Adapters;

namespace Wirehome.Core.Hardware.I2C
{
    public class I2CBusService : IService
    {
        private readonly Dictionary<string, II2CBusAdapter> _adapters = new Dictionary<string, II2CBusAdapter>();

        private readonly ILogger _logger;

        public I2CBusService(ILogger<I2CBusService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var i2CAdapter = new LinuxI2CBusAdapter(1, _logger);
                i2CAdapter.Enable();
                RegisterAdapter(string.Empty, i2CAdapter);
            }
            else
            {
                var i2CAdapter = new TestI2CBusAdapter(_logger);
                RegisterAdapter(string.Empty, i2CAdapter);
            }
        }

        public void RegisterAdapter(string busId, II2CBusAdapter adapter)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            _adapters[busId] = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger.Log(LogLevel.Information, $"Registered I2C bus ID '{busId}'.");
        }

        public void Write(string busId, int deviceAddress, ArraySegment<byte> buffer)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            GetAdapter(busId).Write(deviceAddress, buffer);
        }

        public void Read(string busId, int deviceAddress, ArraySegment<byte> buffer)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            GetAdapter(busId).Read(deviceAddress, buffer);
        }

        public void WriteRead(string busId, int deviceAddress, ArraySegment<byte> writeBuffer, ArraySegment<byte> readBuffer)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            GetAdapter(busId).WriteRead(deviceAddress, writeBuffer, readBuffer);
        }

        private II2CBusAdapter GetAdapter(string busId)
        {
            if (!_adapters.TryGetValue(busId, out var adapter))
            {
                throw new ConfigurationException($"I2C adapter '{busId}' not registered.");
            }

            return adapter;
        }
    }
}
