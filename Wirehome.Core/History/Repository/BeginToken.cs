using System;

namespace Wirehome.Core.History.Repository;

public class BeginToken : Token
{
    public BeginToken(TimeSpan value)
    {
        Value = value;
    }

    public TimeSpan Value { get; }
}