namespace Wirehome.Core.Scheduler
{
    public class TimerTickCallbackParameters
    {
        public TimerTickCallbackParameters(string timerUid, int elapsedMillis, object state)
        {
            TimerUid = timerUid;
            ElapsedMillis = elapsedMillis;
            State = state;
        }

        public string TimerUid { get; }

        public int ElapsedMillis { get; }

        public object State { get; }
    }
}