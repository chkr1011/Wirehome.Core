using System;
using System.Collections.Generic;

namespace Wirehome.Core.Python.Models
{
    // TODO: Convert to "ExceptionModel" and add implicit operator for conversion to dictionaries.
    // TODO: Or use PythonConvert or WirehomeConvert.
    public sealed class ExceptionPythonModel
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

        public IDictionary<object, object> ToDictionary()
        {
            return new Dictionary<object, object>
            {
                ["type"] = Type,
                ["exception.type"] = ExceptionType,
                ["exception.message"] = Message,
                ["exception.stack_trace"] = StackTrace,
            };
        }

        static string GetTypeFromException(Exception exception)
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
