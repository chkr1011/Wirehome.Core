using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Wirehome.Core.HTTP.Filters
{
    /// <summary>
    /// Configures operations decorated with the <see cref="BinaryContentAttribute" />.
    /// </summary>
    public class BinaryContentFilter : IOperationFilter
    {
        /// <summary>
        /// Configures operations decorated with the <see cref="BinaryContentAttribute" />.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="context">The context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null) return;

            if (context.MethodInfo.GetCustomAttributes(typeof(BinaryContentAttribute), false).Any())
            {
                operation.RequestBody = new OpenApiRequestBody();
                operation.RequestBody.Content.Add("application/octet-stream", new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema()
                    {
                        Type = "string",
                        Format = "binary",
                    },
                });
                operation.RequestBody.Content.Add("text/plain", new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema()
                    {
                        Type = "string",
                        Format = "string",
                    },
                });
            }
        }
    }
}
