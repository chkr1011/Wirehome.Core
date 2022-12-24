namespace Wirehome.Core.History.Repository;

public class ValueToken : Token
{
    public ValueToken(string value)
    {
        Value = value;
    }

    public string Value { get; }
}