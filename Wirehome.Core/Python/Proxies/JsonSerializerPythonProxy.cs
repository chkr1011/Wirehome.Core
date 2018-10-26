#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Python.Proxies
{
    public class JsonSerializerPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "json_serializer";

        public object deserialize(object value)
        {
            var jsonText = new ConverterPythonProxy().to_string(value);
            var json = JToken.Parse(jsonText);

            return PythonConvert.ToPython(json);
        }

        public string serialize(object value)
        {
            var json = PythonConvert.FromPythonToJson(value);
            return json.ToString(Formatting.None);
        }

        public string serialize_indented(object value)
        {
            var json = PythonConvert.FromPythonToJson(value);
            return json.ToString(Formatting.Indented);
        }

        [Obsolete]
        public object deserialize_json(object value)
        {
            return deserialize(value);
        }
    }
}
