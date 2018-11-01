using System;

namespace Wirehome.Cloud.Services.Exceptions
{
    public class SessionNotFoundException : Exception
    {
        public SessionNotFoundException(string key) : base($"Session '{key}' not found.")
        {
            
        }
    }
}
