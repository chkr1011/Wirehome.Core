namespace Wirehome.Core.Scheduler;

public sealed class TimerTickCallbackParameters
{
    public TimerTickCallbackParameters(string timerUid, int elapsedMillis, object state)
    {
        TimerUid = timerUid;
        ElapsedMillis = elapsedMillis;
        State = state;
    }

    public int ElapsedMillis { get; }

    public object State { get; }

    public string TimerUid { get; }
}