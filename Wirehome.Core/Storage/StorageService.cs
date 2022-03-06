using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage;

public sealed class StorageService : WirehomeCoreService
{
    const string JsonExtension = ".json";

    readonly JsonSerializerService _jsonSerializerService;
    readonly ILogger<StorageService> _logger;

    public StorageService(JsonSerializerService jsonSerializerService, ILogger<StorageService> logger)
    {
        _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
        _logger = logger;

        var paths = new StoragePaths();
        BinPath = paths.BinPath;
        DataPath = paths.DataPath;

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        logger.Log(LogLevel.Information, $"Bin path  = {BinPath}");
        logger.Log(LogLevel.Information, $"Data path = {DataPath}");
    }

    public string BinPath { get; }

    public string DataPath { get; }

    public void DeletePath(params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var fullPath = Path.Combine(DataPath, Path.Combine(path));

        try
        {
            File.Delete(fullPath);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (FileNotFoundException)
        {
        }

        try
        {
            Directory.Delete(fullPath, true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    public List<string> EnumerateDirectories(string pattern, params string[] path)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var directory = Path.Combine(DataPath, Path.Combine(path));
        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        var directories = Directory.GetDirectories(directory, pattern, SearchOption.TopDirectoryOnly).ToList();
        for (var i = 0; i < directories.Count; i++)
        {
            // Remove the root path so that other services will only see the relative path of the directory.
            directories[i] = directories[i].Replace(directory, string.Empty, StringComparison.Ordinal).TrimStart(Path.DirectorySeparatorChar);
        }

        return directories;
    }

    public List<string> EnumerateFiles(string pattern, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var relativePath = Path.Combine(path);
        var directory = Path.Combine(DataPath, relativePath);

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).ToList();
        for (var i = 0; i < files.Count; i++)
        {
            // Remove the root path so that other services will only see the relative path of the file.
            files[i] = files[i].Replace(directory, string.Empty, StringComparison.Ordinal).TrimStart(Path.DirectorySeparatorChar);
        }

        return files;
    }

    public bool SafeReadSerializedValue<TValue>(out TValue value, params string[] path) where TValue : class, new()
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!TryReadSerializedValue(out value, path))
        {
            value = new TValue();
            WriteSerializedValue(value, path);
            return false;
        }

        return true;
    }

    public bool TryReadRawBytes(out byte[] content, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(DataPath, Path.Combine(path));

        try
        {
            content = File.ReadAllBytes(filename);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            content = null;
            return false;
        }
        catch (FileNotFoundException)
        {
            content = null;
            return false;
        }
    }

    public bool TryReadRawText(out string content, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(DataPath, Path.Combine(path));

        try
        {
            content = File.ReadAllText(filename, Encoding.UTF8);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            content = null;
            return false;
        }
        catch (FileNotFoundException)
        {
            content = null;
            return false;
        }
    }

    public bool TryReadSerializedValue<TValue>(out TValue value, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(path);
        try
        {
            if (!filename.EndsWith(JsonExtension, StringComparison.Ordinal))
            {
                filename += JsonExtension;
            }

            if (!TryReadRawText(out var textValue, filename))
            {
                value = default;
                return false;
            }

            value = _jsonSerializerService.Deserialize<TValue>(textValue);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, $"Error while reading serialized value '{filename}'.");

            value = default;
            return false;
        }
    }

    public void WriteRawBytes(byte[] content, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(DataPath, Path.Combine(path));
        var directory = Path.GetDirectoryName(filename);

        Directory.CreateDirectory(directory);
        File.WriteAllBytes(filename, content ?? Array.Empty<byte>());
    }

    public void WriteRawText(string value, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(DataPath, Path.Combine(path));
        var directory = Path.GetDirectoryName(filename);

        Directory.CreateDirectory(directory);
        File.WriteAllText(filename, value ?? string.Empty, Encoding.UTF8);
    }

    public void WriteSerializedValue(object value, params string[] path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var filename = Path.Combine(DataPath, Path.Combine(path));
        if (!filename.EndsWith(JsonExtension, StringComparison.Ordinal))
        {
            filename += JsonExtension;
        }

        var directory = Path.GetDirectoryName(filename);
        Directory.CreateDirectory(directory);

        if (value == null)
        {
            File.WriteAllBytes(filename, Array.Empty<byte>());
            return;
        }

        var json = _jsonSerializerService.Serialize(value);
        File.WriteAllText(filename, json, Encoding.UTF8);
    }
}