using IronPython.Runtime;
using System;
using Wirehome.Core.Constants;

namespace Wirehome.Core.Components.Adapters
{
    public class EmptyComponentAdapter : IComponentAdapter
    {
        public Func<PythonDictionary, PythonDictionary> MessagePublishedCallback { get; set; }

        public PythonDictionary ProcessMessage(PythonDictionary parameters)
        {
            return new PythonDictionary
            {
                ["type"] = ControlType.NotSupportedException
            };
        }
    }
}
