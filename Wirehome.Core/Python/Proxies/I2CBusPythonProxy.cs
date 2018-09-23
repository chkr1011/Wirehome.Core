#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Extensions;
using Wirehome.Core.Hardware.I2C;

namespace Wirehome.Core.Python.Proxies
{
    public class I2CBusPythonProxy : IPythonProxy
    {
        private readonly I2CBusService _i2CBusService;

        public I2CBusPythonProxy(I2CBusService i2CBusService)
        {
            _i2CBusService = i2CBusService ?? throw new ArgumentNullException(nameof(i2CBusService));
        }

        public string ModuleName { get; } = "i2c";

        public void write(string busId, int deviceAddress, List buffer)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            _i2CBusService.Write(busId, deviceAddress, ConverterPythonProxy.ListToByteArray(buffer).AsArraySegment());
        }

        public void write_as_ulong(string busId, int deviceAddress, ulong buffer, int bufferLength)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            var buffer2 = ConverterPythonProxy.ULongToArray(buffer, bufferLength);
            _i2CBusService.Write(busId, deviceAddress, buffer2.AsArraySegment());
        }

        // TODO: Use list as result!
        public byte[] read(string busId, int deviceAddress, int count)
        {
            var buffer = new byte[count];
            _i2CBusService.Read(busId, deviceAddress, buffer.AsArraySegment());
            return buffer;
        }

        public ulong read_as_ulong(string busId, int deviceAddress, int count)
        {
            var buffer = new byte[count];
            _i2CBusService.Write(busId, deviceAddress, buffer.AsArraySegment());
            return ConverterPythonProxy.ArrayToULong(buffer);
        }

        // TODO: Use list as result!
        public byte[] write_read(string busId, int deviceAddress, List writeBuffer, int readBufferLength)
        {
            var readBuffer = new byte[readBufferLength];
            _i2CBusService.WriteRead(
                busId, 
                deviceAddress,
                ConverterPythonProxy.ListToByteArray(writeBuffer).AsArraySegment(),
                readBuffer.AsArraySegment());

            return readBuffer;
        }

        public ulong write_read_as_ulong(string busId, int deviceAddress, ulong writeBuffer, int writeBufferLength, int readBufferLength)
        {
            var writeBuffer2 = ConverterPythonProxy.ULongToArray(writeBuffer, writeBufferLength);
            var readBuffer = new byte[readBufferLength];

            _i2CBusService.WriteRead(
                busId,
                deviceAddress,
                writeBuffer2.AsArraySegment(),
                readBuffer.AsArraySegment());

            return ConverterPythonProxy.ArrayToULong(readBuffer);
        }

       
    }
}

#pragma warning restore IDE1006 // Naming Styles