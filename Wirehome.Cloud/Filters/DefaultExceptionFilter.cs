using System;
using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Wirehome.Cloud.Filters
{
    public class DefaultExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is UnauthorizedAccessException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.ExceptionHandled = true;
                return;
            }

            base.OnException(context);
        }
    }
}
