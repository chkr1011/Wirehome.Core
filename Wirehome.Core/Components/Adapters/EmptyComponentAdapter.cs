using System;
using IronPython.Runtime;
using Wirehome.Core.Constants;

namespace Wirehome.Core.Components.Adapters;

public sealed class EmptyComponentAdapter : IComponentAdapter
{
    public Func<PythonDictionary, PythonDictionary> MessagePublishedCallback { get; set; }

    public PythonDictionary ProcessMessage(PythonDictionary parameters)
    {
        return new PythonDictionary
        {
            ["type"] = WirehomeMessageType.NotSupportedException
        };
    }
}