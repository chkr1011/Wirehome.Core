using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage;

public sealed class JsonSerializerService : WirehomeCoreService
{
    readonly JsonSerializerSettings _serializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        },
        Formatting = Formatting.Indented,
        DateParseHandling = DateParseHandling.None,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    readonly object _syncRoot = new();

    public TValue Deserialize<TValue>(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        lock (_syncRoot)
        {
            return JsonConvert.DeserializeObject<TValue>(json, _serializerSettings);
        }
    }

    public string Serialize(object value)
    {
        lock (_syncRoot)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }
    }
}