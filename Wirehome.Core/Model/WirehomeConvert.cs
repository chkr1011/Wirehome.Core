using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Model
{
    public static class WirehomeConvert
    {
        //public static WirehomeDictionary FromJson(string json)
        //{

        //}

        public static object FromJson(JToken json)
        {
            if (json == null)
            {
                return null;
            }

            if (json is JObject jsonObject)
            {
                var dictionary = new WirehomeDictionary();
                foreach (var property in jsonObject.Properties())
                {
                    dictionary[property.Name] = FromJson(property.Value);
                }

                return dictionary;
            }

            if (json is JArray jsonArray)
            {
                var list = new List<object>();
                foreach (var item in jsonArray)
                {
                    list.Add(FromJson(item));
                }
            }

            return json.ToObject<object>();
        }
    }
}
