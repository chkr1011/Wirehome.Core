#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
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

    public List read(string bus_id, int device_address, int count)
    {
        var buffer = new byte[count];
        var result = _i2CBusService.Read(bus_id, device_address, buffer);
        if (result == -1)
        {
            // Return an empty list to indicate failure.
            return new List();
        }
        
        return PythonConvert.ToPythonList(buffer);
    }

    public ulong read_as_ulong(string bus_id, int device_address, int count)
    {
        var buffer = new byte[count];
        var result = _i2CBusService.Read(bus_id, device_address, buffer);
        if (result == -1)
        {
            // Return a constant value to indicate failure.
            return ulong.MaxValue;
        }
        
        return ConverterPythonProxy.ArrayToULong(buffer);
    }

    public int write(string bus_id, int device_address, List buffer)
    {
        if (bus_id == null)
        {
            throw new ArgumentNullException(nameof(bus_id));
        }

        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        return _i2CBusService.Write(bus_id, device_address, ConverterPythonProxy.ListToByteArray(buffer));
    }

    public int write_as_ulong(string bus_id, int device_address, ulong buffer, int buffer_length)
    {
        if (bus_id == null)
        {
            throw new ArgumentNullException(nameof(bus_id));
        }

        var buffer2 = ConverterPythonProxy.ULongToArray(buffer, buffer_length);
        return _i2CBusService.Write(bus_id, device_address, buffer2);
    }

    public List write_read(string bus_id, int device_address, List write_buffer, int read_buffer_length)
    {
        if (write_buffer is null)
        {
            throw new ArgumentNullException(nameof(write_buffer));
        }

        var readBuffer = new byte[read_buffer_length];
        var result = _i2CBusService.WriteRead(bus_id, device_address, ConverterPythonProxy.ListToByteArray(write_buffer), readBuffer);

        if (result == -1)
        {
            // Return a list with 0 items to indicate failure.
            return new List();
        }

        return PythonConvert.ToPythonList(readBuffer);
    }

    public ulong write_read_as_ulong(string bus_id, int device_address, ulong write_buffer, int write_buffer_length, int read_buffer_length)
    {
        var writeBuffer2 = ConverterPythonProxy.ULongToArray(write_buffer, write_buffer_length);
        var readBuffer = new byte[read_buffer_length];

        var result = _i2CBusService.WriteRead(bus_id, device_address, writeBuffer2, readBuffer);
        if (result == -1)
        {
            // Indicate failure by using constant value.
            return ulong.MaxValue;
        }

        return ConverterPythonProxy.ArrayToULong(readBuffer);
    }
}