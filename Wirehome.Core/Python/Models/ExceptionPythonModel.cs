using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python.Models
{
    public class ExceptionPythonModel : TypedWirehomeDictionary
    {
        public ExceptionPythonModel(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Type = GetTypeFromException(exception);
            ExceptionType = exception.GetType().Name;
            Message = exception.Message;
            StackTrace = exception.StackTrace;
        }

        public string Type { get; }

        public string ExceptionType { get; }

        public string Message { get; }

        public string StackTrace { get; }

        private static string GetTypeFromException(Exception exception)
        {
            if (exception is InvalidOperationException)
            {
                return "exception.invalid_operation";
            }

            if (exception is NotSupportedException)
            {
                return "exception.not_supported";
            }

            if (exception is NotImplementedException)
            {
                return "exception.not_implemented";
            }

            if (exception is ArgumentNullException)
            {
                return "exception.parameter_invalid";
            }

            if (exception is TimeoutException)
            {
                return "exception.timeout";
            }

            return "exception";
        }
    }
}
