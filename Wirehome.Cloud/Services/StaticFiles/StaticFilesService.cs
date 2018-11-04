using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Wirehome.Cloud.Services.StaticFiles
{
    public class StaticFilesService
    {
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();
        private readonly string _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StaticFiles");
        
        public async Task<bool> HandleRequestAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var filename = Path.Combine(_rootPath, httpContext.Request.Path.ToString().Replace("/", "\\").Substring(1));
            if (!File.Exists(filename))
            {
                return false;
            }

            if (_contentTypeProvider.TryGetContentType(filename, out var contentType))
            {
                httpContext.Response.ContentType = contentType;
            }

            using (var fileStream = File.OpenRead(filename))
            {
                await fileStream.CopyToAsync(httpContext.Response.Body).ConfigureAwait(false);
            }
            
            return true;
        }
    }
}
