using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage
{
    public class JsonSerializerService : WirehomeCoreService
    {
        readonly object _syncRoot = new object();
        readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public string Serialize(object value)
        {
            lock (_syncRoot)
            {
                return JsonConvert.SerializeObject(value, _serializerSettings);
            }
        }

        public TValue Deserialize<TValue>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            lock (_syncRoot)
            {
                return JsonConvert.DeserializeObject<TValue>(json, _serializerSettings);
            }
        }
    }
}
