using System;
using System.Linq.Expressions;

namespace Wirehome.Core.Exceptions
{
    public static class ExceptionHelper
    {
        public static void ThrowNativeHardwareExceptionIfNoSuccess(Expression<Func<int>> action)
        {
            var errorCode = action.Compile().Invoke();
            if (errorCode == 0)
            {
                return;
            }

            var message = action.ToString() + " failed (ErrorCode = " + errorCode + ").";
            throw new WirehomeHardwareException(message, errorCode);
        }

        public static void ThrowNativeHardwareExceptionIfNoSuccess(Func<int> action, string message)
        {
            var errorCode = action();
            if (errorCode == 0)
            {
                return;
            }

            message += " (ErrorCode = " + errorCode + ")";
            throw new WirehomeHardwareException(message, errorCode);
        }
    }
}
