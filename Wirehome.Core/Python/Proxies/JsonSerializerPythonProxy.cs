#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Python.Proxies
{
    public class JsonSerializerPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "json_serializer";

        public object deserialize_json(object source)
        {
            var jsonText = new ConverterPythonProxy().to_string(source);
            var json = JToken.Parse(jsonText);
            return PythonConvert.ToPython(json);
        }
    }
}
