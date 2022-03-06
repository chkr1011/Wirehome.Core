using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using IronPython.Runtime;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Constants;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.HTTP.PythonProxies;

public sealed class HttpClientPythonProxy : IInjectedPythonProxy, IDisposable
{
    readonly HttpClient _httpClient = new();

    public string ModuleName { get; } = "http_client";

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public PythonDictionary send(PythonDictionary parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        try
        {
            using (var request = CreateRequest(parameters))
            {
                var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                var content = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                var result = new PythonDictionary
                {
                    ["type"] = WirehomeMessageType.Success,
                    ["status_code"] = (int)response.StatusCode
                };

                var responseContentType = Convert.ToString(parameters.get("response_content_type", "text"));
                if (responseContentType == "raw")
                {
                    var rawContent = new List();
                    foreach (var contentByte in content)
                    {
                        rawContent.Add(contentByte);
                    }

                    result["content"] = rawContent;
                }
                else if (responseContentType == "text")
                {
                    result["content"] = Encoding.UTF8.GetString(content);
                }
                else if (responseContentType == "json")
                {
                    var jsonString = Encoding.UTF8.GetString(content);
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        try
                        {
                            var json = JObject.Parse(jsonString);

                            var convertedJson = PythonConvert.ToPython(json);
                            result["content"] = convertedJson;
                        }
                        catch (Exception exception)
                        {
                            return PythonConvert.ToPythonDictionary(new ExceptionPythonModel(exception).ToDictionary());
                        }
                    }
                }

                return result;
            }
        }
        catch (Exception exception)
        {
            return PythonConvert.ToPythonDictionary(new ExceptionPythonModel(exception).ToDictionary());
        }
    }

    static HttpRequestMessage CreateRequest(PythonDictionary parameters)
    {
        var uri = Convert.ToString(parameters.get("uri"));
        var method = Convert.ToString(parameters.get("method", "get"));
        var headers = (PythonDictionary)parameters.get("headers", new PythonDictionary());

        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(method),
            RequestUri = new Uri(uri, UriKind.Absolute)
        };

        foreach (var header in headers)
        {
            var headerName = Convert.ToString(header.Key, CultureInfo.InvariantCulture);
            var headerValue = Convert.ToString(header.Value, CultureInfo.InvariantCulture);

            request.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        return request;
    }
}