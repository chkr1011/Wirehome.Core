using System;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public interface II2CBusAdapter
    {
        void Read(int deviceAddress, Span<byte> buffer);
        void Write(int deviceAddress, ReadOnlySpan<byte> buffer);

        void WriteRead(int deviceAddress, ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer);
    }
}