using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Extensions;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class HttpClientPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "http_client";

        public IDictionary send(IDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var uri = parameters["uri"] as string;
            var method = parameters["method"] as string;
            var responseContentType = parameters.GetValueOrDefault("response_content_type", "text") as string;

            var result = new Dictionary<object, object>();
            
            using (var httpClient = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                if (method == "post")
                {
                    request.Method = HttpMethod.Post;
                }
                else if (method == "delete")
                {
                    request.Method = HttpMethod.Delete;
                }
                else
                {
                    request.Method = HttpMethod.Get;
                }

                request.RequestUri = new Uri(uri, UriKind.Absolute);

                var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                result["status_code"] = (int)response.StatusCode;

                if (responseContentType == "text")
                {
                    result["content"] = content;
                }
                else if (responseContentType == "json")
                {
                    if (!string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            var json = JObject.Parse(content);
                            var convertedJson = PythonConvert.ForPython(json);
                            result["content"] = convertedJson;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return result;
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles