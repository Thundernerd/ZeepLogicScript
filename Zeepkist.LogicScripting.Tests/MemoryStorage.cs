using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ZeepSDK.Storage;

namespace TNRD.Zeepkist.LogicScripting.Tests;

/// <summary>
/// An <see cref="IModStorage"/> that keeps its JSON files in memory and reads and writes them with Json.NET, as
/// ZeepSDK's storage does.
/// </summary>
internal sealed class MemoryStorage : IModStorage
{
    private readonly Dictionary<string, string> _jsonFiles = new();

    public string ReadJson(string name)
    {
        return _jsonFiles[name];
    }

    public void WriteJson(string name, string json)
    {
        _jsonFiles[name] = json;
    }

    public bool JsonFileExists(string name)
    {
        return _jsonFiles.ContainsKey(name);
    }

    public void SaveToJson(string name, object data)
    {
        _jsonFiles[name] = JsonConvert.SerializeObject(data);
    }

    public StorageResult<TData> TryLoadFromJson<TData>(string name)
    {
        try
        {
            var value = JsonConvert.DeserializeObject<TData>(_jsonFiles[name]);
            return (StorageResult<TData>)StorageResultFactory<TData>("FromValue").Invoke(null, [value]);
        }
        catch (JsonException exception)
        {
            return (StorageResult<TData>)StorageResultFactory<TData>("FromValueFailure")
                .Invoke(null, ["Failed to load JSON.", exception]);
        }
    }

    // StorageResult has no public constructor, so this calls the factories ZeepSDK uses itself
    private static MethodInfo StorageResultFactory<TData>(string name)
    {
        return typeof(StorageResult<TData>).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(StorageResult<TData>).FullName, name);
    }

    public TData LoadFromJson<TData>(string name)
    {
        return TryLoadFromJson<TData>(name).Value;
    }

    public void AddConverter(JsonConverter converter)
    {
        throw new NotSupportedException();
    }

    public void RemoveConverter(JsonConverter converter)
    {
        throw new NotSupportedException();
    }

    public object LoadFromJson(string name)
    {
        throw new NotSupportedException();
    }

    public object LoadFromJson(string name, Type type)
    {
        throw new NotSupportedException();
    }

    [Obsolete("use DeleteJsonFile or DeleteBlob instead.")]
    public void DeleteFile(string name)
    {
        throw new NotSupportedException();
    }

    public void DeleteJsonFile(string name)
    {
        throw new NotSupportedException();
    }

    public bool BlobFileExists(string name)
    {
        throw new NotSupportedException();
    }

    public void WriteBlob(string name, byte[] data)
    {
        throw new NotSupportedException();
    }

    public Task<StorageResult> WriteBlobAsync(string name, byte[] data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public byte[] ReadBlob(string name)
    {
        throw new NotSupportedException();
    }

    public StorageResult<byte[]> TryReadBlob(string name)
    {
        throw new NotSupportedException();
    }

    public Task<StorageResult<byte[]>> ReadBlobAsync(string name, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public void DeleteBlob(string name)
    {
        throw new NotSupportedException();
    }

    public void SaveToFile(string filename, byte[] data)
    {
        throw new NotSupportedException();
    }
}
