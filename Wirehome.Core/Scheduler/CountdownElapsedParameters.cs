namespace Wirehome.Core.Scheduler;

public sealed class CountdownElapsedParameters
{
    public CountdownElapsedParameters(string countdownUid, object state)
    {
        CountdownUid = countdownUid;
        State = state;
    }

    public string CountdownUid { get; }

    public object State { get; }
}