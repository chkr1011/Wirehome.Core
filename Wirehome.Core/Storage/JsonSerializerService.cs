using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Text;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage
{
    public class JsonSerializerService : IService
    {
        readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented,
            DateParseHandling = DateParseHandling.None
        };

        public void Start()
        {
        }

        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }

        public TValue Deserialize<TValue>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            return JsonConvert.DeserializeObject<TValue>(json, _serializerSettings);
        }

        public bool TryDeserializeFile<TContent>(string filename, out TContent content)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            try
            {
                var buffer = File.ReadAllText(filename, Encoding.UTF8);
                content = Deserialize<TContent>(buffer);
                return true;
            }
            catch (Exception)
            {
                content = default;
                return false;
            }
        }
    }
}
