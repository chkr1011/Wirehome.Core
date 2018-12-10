using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.HTTP.Filters
{
    public class NotFoundExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is NotFoundException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.ExceptionHandled = true;
                return;
            }

            base.OnException(context);
        }
    }
}
