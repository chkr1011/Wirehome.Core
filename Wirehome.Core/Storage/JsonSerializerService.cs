using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Wirehome.Core.Storage
{
    public class JsonSerializerService
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented,
            DateParseHandling = DateParseHandling.None
        };

        private readonly JsonSerializer _serializer;

        public JsonSerializerService()
        {
            _serializer = JsonSerializer.Create(_serializerSettings);
        }

        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }

        public TValue Deserialize<TValue>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using (var streamReader = new StringReader(json))
            {
                return (TValue)_serializer.Deserialize(streamReader, typeof(TValue));
            }
        }

        public TValue Deserialize<TValue>(byte[] json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using (var streamReader = new StreamReader(new MemoryStream(json), Encoding.UTF8))
            {
                return (TValue)_serializer.Deserialize(streamReader, typeof(TValue));
            }
        }
    }
}
