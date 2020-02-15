using System;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public interface II2CBusAdapter
    {
        void Write(int deviceAddress, ReadOnlySpan<byte> buffer);

        void Read(int deviceAddress, Span<byte> buffer);

        void WriteRead(int deviceAddress, ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer);
    }
}
