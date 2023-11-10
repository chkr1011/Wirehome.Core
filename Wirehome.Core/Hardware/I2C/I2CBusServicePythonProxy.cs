#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Buffers;
using IronPython.Runtime;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Hardware.I2C;

public sealed class I2CBusServicePythonProxy : IInjectedPythonProxy
{
    readonly I2CBusService _i2CBusService;

    public I2CBusServicePythonProxy(I2CBusService i2CBusService)
    {
        _i2CBusService = i2CBusService ?? throw new ArgumentNullException(nameof(i2CBusService));
    }

    public string ModuleName => "i2c";

    public PythonList read(string bus_id, int device_address, int length)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var result = _i2CBusService.Read(bus_id, device_address, buffer, length);
            if (result == -1)
            {
                // Return an empty list to indicate failure.
                return new PythonList();
            }

            return PythonConvert.ToPythonList(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public ulong read_as_ulong(string bus_id, int device_address, int length)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var result = _i2CBusService.Read(bus_id, device_address, buffer, length);
            if (result == -1)
            {
                // Return a constant value to indicate failure.
                return ulong.MaxValue;
            }

            return ConverterPythonProxy.ArrayToULong(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public int write(string bus_id, int device_address, PythonList value)
    {
        if (bus_id == null)
        {
            throw new ArgumentNullException(nameof(bus_id));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(value.Count);
        try
        {
            ConverterPythonProxy.ListToByteArray(value, buffer);
            return _i2CBusService.Write(bus_id, device_address, buffer, value.Count);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public int write_as_ulong(string bus_id, int device_address, ulong value, int buffer_length)
    {
        if (bus_id == null)
        {
            throw new ArgumentNullException(nameof(bus_id));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(buffer_length);
        try
        {
            ConverterPythonProxy.ULongToArray(value, buffer, buffer_length);
            return _i2CBusService.Write(bus_id, device_address, buffer, buffer_length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public PythonList write_read(string bus_id, int device_address, PythonList write_buffer, int read_buffer_length)
    {
        if (write_buffer is null)
        {
            throw new ArgumentNullException(nameof(write_buffer));
        }

        var writeBuffer = ArrayPool<byte>.Shared.Rent(write_buffer.Count);
        var readBuffer = ArrayPool<byte>.Shared.Rent(read_buffer_length);
        try
        {
            ConverterPythonProxy.ListToByteArray(write_buffer, writeBuffer);
            var result = _i2CBusService.WriteRead(bus_id, device_address, writeBuffer, write_buffer.Count, readBuffer, read_buffer_length);

            if (result == -1)
            {
                // Return a list with 0 items to indicate failure.
                return new PythonList();
            }

            return PythonConvert.ToPythonList(readBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(writeBuffer);
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    public ulong write_read_as_ulong(string bus_id, int device_address, ulong write_buffer, int write_buffer_length, int read_buffer_length)
    {
        var writeBuffer = ArrayPool<byte>.Shared.Rent(write_buffer_length);
        var readBuffer = ArrayPool<byte>.Shared.Rent(read_buffer_length);
        try
        {
            ConverterPythonProxy.ULongToArray(write_buffer, writeBuffer, write_buffer_length);

            var result = _i2CBusService.WriteRead(bus_id, device_address, writeBuffer, write_buffer_length, readBuffer, read_buffer_length);
            if (result == -1)
            {
                // Indicate failure by using constant value.
                return ulong.MaxValue;
            }

            return ConverterPythonProxy.ArrayToULong(readBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(writeBuffer);
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }
}