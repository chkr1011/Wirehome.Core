using IronPython.Runtime;
using System;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;

namespace Wirehome.Core.Components.Logic
{
    public class EmptyComponentLogic : IComponentLogic
    {
        readonly IComponentAdapter _adapter;

        public EmptyComponentLogic(IComponentAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public PythonDictionary ProcessMessage(PythonDictionary parameters)
        {
            return _adapter.ProcessMessage(parameters);
        }

        public PythonDictionary GetDebugInformation(PythonDictionary parameters)
        {
            return new PythonDictionary
            {
                ["type"] = WirehomeMessageType.NotSupportedException
            };
        }
    }
}
