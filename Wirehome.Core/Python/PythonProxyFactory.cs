using System.Collections.Generic;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Python.Proxies.OS;

namespace Wirehome.Core.Python
{
    public class PythonProxyFactory
    {
        public List<IPythonProxy> CreateDefaultProxies()
        {
            return new List<IPythonProxy>
            {
                new ConverterPythonProxy(),
                new DateTimePythonProxy(),
                new DateTimeParserPythonProxy(),
                new DataProviderPythonProxy(),
                new JsonSerializerPythonProxy(),
                new OSPythonProxy()
            };
        }
    }
}
