using System;

namespace Wirehome.Core.History.Repository;

public class EndToken : Token
{
    public EndToken(TimeSpan value)
    {
        Value = value;
    }

    public TimeSpan Value { get; }
}