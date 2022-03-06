#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Storage;

public sealed class ValueStoragePythonProxy : IInjectedPythonProxy
{
    readonly ValueStorageService _valueStorageService;

    public ValueStoragePythonProxy(ValueStorageService valueStorageService)
    {
        _valueStorageService = valueStorageService ?? throw new ArgumentNullException(nameof(valueStorageService));
    }

    public string ModuleName => "value_storage";

    public void delete(string path)
    {
        _valueStorageService.Delete(RelativeValueStoragePath.Parse(path));
    }

    public object read(string path, object defaultValue = null)
    {
        if (_valueStorageService.TryRead<object>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public bool read_bool(string path, bool defaultValue = false)
    {
        if (_valueStorageService.TryRead<bool>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public float read_float(string path, float defaultValue = 0.0F)
    {
        if (_valueStorageService.TryRead<float>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public int read_int(string path, int defaultValue = 0)
    {
        if (_valueStorageService.TryRead<int>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public PythonDictionary read_object(string path, PythonDictionary defaultValue = null)
    {
        if (_valueStorageService.TryRead<IDictionary<object, object>>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return PythonConvert.ToPythonDictionary(value);
        }

        return defaultValue;
    }

    public string read_string(string path, string defaultValue = null)
    {
        if (_valueStorageService.TryRead<string>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public void write(string path, object value)
    {
        _valueStorageService.Write(RelativeValueStoragePath.Parse(path), value);
    }
}