#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
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

            _i2CBusService.Write(busId, deviceAddress, ListToByteArray(buffer).AsArraySegment());
        }

        public void write_as_ulong(string busId, int deviceAddress, ulong buffer, int bufferLength)
        {
            if (busId == null) throw new ArgumentNullException(nameof(busId));

            var buffer2 = ULongToArray(buffer, bufferLength);
            _i2CBusService.Write(busId, deviceAddress, buffer2.AsArraySegment());
        }

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
            return BufferToULong(buffer);
        }

        public byte[] write_read(string busId, int deviceAddress, List writeBuffer, int readBufferLength)
        {
            var readBuffer = new byte[readBufferLength];
            _i2CBusService.WriteRead(
                busId, 
                deviceAddress,
                ListToByteArray(writeBuffer).AsArraySegment(),
                readBuffer.AsArraySegment());

            return readBuffer;
        }

        public ulong write_read_as_ulong(string busId, int deviceAddress, ulong writeBuffer, int writeBufferLength, int readBufferLength)
        {
            var writeBuffer2 = ULongToArray(writeBuffer, writeBufferLength);
            var readBuffer = new byte[readBufferLength];

            _i2CBusService.WriteRead(
                busId,
                deviceAddress,
                writeBuffer2.AsArraySegment(),
                readBuffer.AsArraySegment());

            return BufferToULong(readBuffer);
        }

        private static byte[] ULongToArray(ulong buffer, int length)
        {
            var result = new byte[length];
            for (var i = 0; i < length; i++)
            {
                result[i] = (byte)(buffer >> (8 * i));
            }

            return result;
        }

        private static ulong BufferToULong(IReadOnlyList<byte> buffer)
        {
            ulong result = 0;
            for (var i = 0; i < buffer.Count; i++)
            {
                result |= (ulong)buffer[i] << (8 * i);
            }

            return result;
        }

        private static byte[] ListToByteArray(List list)
        {
            var buffer = new byte[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                buffer[i] = Convert.ToByte(list[i]);
            }

            return buffer;
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles