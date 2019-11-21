using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;
using Wirehome.Cloud.Services.DeviceConnector;

namespace Wirehome.Cloud.Filters
{
    public class DefaultExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext exceptionContext)
        {
            exceptionContext.ExceptionHandled = HandleException(exceptionContext.Exception, exceptionContext.HttpContext);
            base.OnException(exceptionContext);
        }

        public static bool HandleException(Exception exception, HttpContext httpContext)
        {
            if (exception is UnauthorizedAccessException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return true;
            }

            if (exception is DeviceSessionNotFoundException)
            {
                httpContext.Response.Redirect("/Cloud/Channel/DeviceNotConnected");
                return true;
            }

            return false;
        }
    }
}
