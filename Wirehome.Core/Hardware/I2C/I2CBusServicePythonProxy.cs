#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Extensions;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Hardware.I2C
{
    public class I2CBusServicePythonProxy : IInjectedPythonProxy
    {
        readonly I2CBusService _i2CBusService;

        public I2CBusServicePythonProxy(I2CBusService i2CBusService)
        {
            _i2CBusService = i2CBusService ?? throw new ArgumentNullException(nameof(i2CBusService));
        }

        public string ModuleName { get; } = "i2c";

        public void write(string bus_id, int device_address, List buffer)
        {
            if (bus_id == null) throw new ArgumentNullException(nameof(bus_id));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            _i2CBusService.Write(bus_id, device_address, ConverterPythonProxy.ListToByteArray(buffer).AsArraySegment());
        }

        public void write_as_ulong(string bus_id, int device_address, ulong buffer, int buffer_length)
        {
            if (bus_id == null) throw new ArgumentNullException(nameof(bus_id));

            var buffer2 = ConverterPythonProxy.ULongToArray(buffer, buffer_length);
            _i2CBusService.Write(bus_id, device_address, buffer2.AsArraySegment());
        }

        public List read(string bus_id, int device_address, int count)
        {
            var buffer = new byte[count];
            _i2CBusService.Read(bus_id, device_address, buffer.AsArraySegment());
            return PythonConvert.ToPythonList(buffer);
        }

        public ulong read_as_ulong(string bus_id, int device_address, int count)
        {
            var buffer = new byte[count];
            _i2CBusService.Write(bus_id, device_address, buffer.AsArraySegment());
            return ConverterPythonProxy.ArrayToULong(buffer);
        }

        public List write_read(string bus_id, int device_address, List write_buffer, int read_buffer_length)
        {
            var readBuffer = new byte[read_buffer_length];
            _i2CBusService.WriteRead(
                bus_id,
                device_address,
                ConverterPythonProxy.ListToByteArray(write_buffer).AsArraySegment(),
                readBuffer.AsArraySegment());

            return PythonConvert.ToPythonList(readBuffer);
        }

        public ulong write_read_as_ulong(string bus_id, int device_address, ulong write_buffer, int write_buffer_length, int read_buffer_length)
        {
            var writeBuffer2 = ConverterPythonProxy.ULongToArray(write_buffer, write_buffer_length);
            var readBuffer = new byte[read_buffer_length];

            _i2CBusService.WriteRead(
                bus_id,
                device_address,
                writeBuffer2.AsArraySegment(),
                readBuffer.AsArraySegment());

            return ConverterPythonProxy.ArrayToULong(readBuffer);
        }
    }
}