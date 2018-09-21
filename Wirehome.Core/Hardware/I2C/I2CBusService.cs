using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Hardware.I2C.Adapters;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Hardware.I2C
{
    public class I2CBusService
    {
        private readonly Dictionary<string, II2CBusAdapter> _adapters = new Dictionary<string, II2CBusAdapter>();

        private readonly ILogger _logger;

        public I2CBusService(PythonEngineService pythonEngineService, ILoggerFactory loggerFactory)
        {
            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));

            _logger = loggerFactory?.CreateLogger<I2CBusService>() ?? throw new ArgumentNullException(nameof(loggerFactory));

            pythonEngineService.RegisterSingletonProxy(new I2CBusPythonProxy(this));
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
                throw new WirehomeConfigurationException($"I2C adapter '{busId}' not registered.");
            }

            return adapter;
        }
    }
}
