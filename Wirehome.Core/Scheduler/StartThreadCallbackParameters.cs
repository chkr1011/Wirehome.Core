namespace Wirehome.Core.Scheduler
{
    public class StartThreadCallbackParameters
    {
        public StartThreadCallbackParameters(string threadUid, object state)
        {
            ThreadUid = threadUid;
            State = state;
        }

        public string ThreadUid { get; }

        public object State { get; }
    }
}