using System;
using IronPython.Runtime;

namespace Wirehome.Core.Components.Adapters;

public interface IComponentAdapter
{
    Func<PythonDictionary, PythonDictionary> MessagePublishedCallback { get; set; }

    PythonDictionary ProcessMessage(PythonDictionary message);
}