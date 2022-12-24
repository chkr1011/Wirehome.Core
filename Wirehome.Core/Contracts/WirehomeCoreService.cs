namespace Wirehome.Core.Contracts;

public abstract class WirehomeCoreService
{
    public void Start()
    {
        OnStart();
    }

    protected virtual void OnStart()
    {
    }
}