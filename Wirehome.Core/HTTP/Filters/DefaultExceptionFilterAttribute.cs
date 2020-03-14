using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.HTTP.Filters
{
    public sealed class DefaultExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext exceptionContext)
        {
            if (exceptionContext is null) throw new ArgumentNullException(nameof(exceptionContext));

            exceptionContext.ExceptionHandled = HandleException(exceptionContext.Exception, exceptionContext.HttpContext);
            base.OnException(exceptionContext);
        }

        public static bool HandleException(Exception exception, HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

            if (exception is UnauthorizedAccessException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return true;
            }

            if (exception is NotFoundException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return true;
            }

            return false;
        }
    }
}
