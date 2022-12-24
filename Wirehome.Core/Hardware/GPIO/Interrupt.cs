using System;
using IronPython.Runtime;

namespace Wirehome.Core.Hardware.GPIO;

public sealed class Interrupt
{
    public Action<PythonDictionary> Callback { get; set; }

    public InterruptEvent Event { get; set; }
    public string GpioHostId { get; set; }

    public int GpioId { get; set; }
}