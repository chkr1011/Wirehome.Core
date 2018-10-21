using System;
using System.Collections.Generic;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Python.Proxies.OS;

namespace Wirehome.Core.Python
{
    public class PythonProxyFactory
    {
        private readonly List<IPythonProxy> _proxies = new List<IPythonProxy>
        {
            new ConverterPythonProxy(),
            new DateTimePythonProxy(),
            new DateTimeParserPythonProxy(),
            new DataProviderPythonProxy(),
            new JsonSerializerPythonProxy(),
            new OSPythonProxy(),
            new ResponseCreatorPythonProxy(),
            new RepositoryPythonProxy()
        };

        public List<IPythonProxy> CreateProxies()
        {
            return new List<IPythonProxy>(_proxies);
        }

        public void RegisterProxy(IPythonProxy pythonProxy)
        {
            if (pythonProxy == null) throw new ArgumentNullException(nameof(pythonProxy));

            _proxies.Add(pythonProxy);
        }
    }
}