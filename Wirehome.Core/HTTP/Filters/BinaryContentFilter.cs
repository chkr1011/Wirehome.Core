using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Wirehome.Core.HTTP.Filters
{
    public sealed class BinaryContentFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null) return;

            if (context.MethodInfo.GetCustomAttributes(typeof(BinaryContentAttribute), false).Any())
            {
                operation.RequestBody = new OpenApiRequestBody();
                
                operation.RequestBody.Content.Add("application/octet-stream", new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                });
                
                operation.RequestBody.Content.Add("text/plain", new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "string"
                    }
                });
            }
        }
    }
}
