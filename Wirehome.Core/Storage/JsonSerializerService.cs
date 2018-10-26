using System;
using System.Text;
using Newtonsoft.Json;

namespace Wirehome.Core.Storage
{
    public class JsonSerializerService
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Formatting = Formatting.Indented
        };

        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _jsonSerializerSettings);
        }

        public TValue Deserialize<TValue>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            return JsonConvert.DeserializeObject<TValue>(json, _jsonSerializerSettings);
        }

        public TValue Deserialize<TValue>(byte[] json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            var buffer = Encoding.UTF8.GetString(json);
            return Deserialize<TValue>(buffer);
        }
    }
}
