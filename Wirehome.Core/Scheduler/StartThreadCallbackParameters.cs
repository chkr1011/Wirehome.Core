namespace Wirehome.Core.Scheduler;

public sealed class StartThreadCallbackParameters
{
    public StartThreadCallbackParameters(string threadUid, object state)
    {
        ThreadUid = threadUid;
        State = state;
    }

    public object State { get; }

    public string ThreadUid { get; }
}