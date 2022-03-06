using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Scripting.Utils;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP;

public sealed class HttpRequestConverter
{
    readonly JsonSerializerService _jsonSerializerService;

    public HttpRequestConverter(JsonSerializerService jsonSerializerService)
    {
        _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
    }

    public void UnwrapContext(IDictionary<object, object> dictionary, HttpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var statusCode = Convert.ToInt32(dictionary.GetValueOrDefault("status_code", (int)HttpStatusCode.NotFound), CultureInfo.InvariantCulture);
        context.Response.StatusCode = statusCode;

        if (dictionary.GetValueOrDefault("headers") is IDictionary<object, object> headers)
        {
            foreach (var header in headers)
            {
                var headerKey = Convert.ToString(header.Key, CultureInfo.InvariantCulture);
                var headerValue = new StringValues(Convert.ToString(header.Value, CultureInfo.InvariantCulture));
                context.Response.Headers.TryAdd(headerKey, headerValue);
            }
        }

        if (dictionary.GetValueOrDefault("content") is IDictionary<object, object> content)
        {
            var type = Convert.ToString(content.GetValueOrDefault("type"), CultureInfo.InvariantCulture);

            byte[] contentBuffer = null;
            if (content.GetValueOrDefault("data") is IDictionary jsonData)
            {
                var serializedData = _jsonSerializerService.Serialize(jsonData);
                contentBuffer = Encoding.UTF8.GetBytes(serializedData);
                type = "application/json";
            }
            else if (content.GetValueOrDefault("data") is IEnumerable list)
            {
                contentBuffer = list.Select(Convert.ToByte).ToArray();
            }

            if (!string.IsNullOrEmpty(type))
            {
                context.Response.ContentType = type;
            }

            if (contentBuffer?.Length > 0)
            {
                context.Response.Body.Write(contentBuffer, 0, contentBuffer.Length);
            }
        }
    }

    public IDictionary<object, object> WrapContext(HttpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var parameters = new Dictionary<object, object>();
        foreach (var parameter in context.Request.Query)
        {
            parameters[parameter.Key] = parameter.Value.ToString();
        }

        var headers = new Dictionary<object, object>();
        foreach (var header in context.Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        byte[] body = null;
        if (context.Request.ContentLength.HasValue)
        {
            body = new byte[context.Request.ContentLength.Value];
            context.Request.Body.Read(body, 0, body.Length);
        }

        return new Dictionary<object, object>
        {
            ["method"] = context.Request.Method.ToUpperInvariant(),
            ["uri"] = context.Request.Path.ToString() + context.Request.QueryString,
            ["path"] = context.Request.Path,
            ["parameters"] = parameters,
            ["headers"] = headers,
            ["content"] = new Dictionary<object, object>
            {
                ["type"] = context.Request.ContentType,
                ["data"] = body
            },
            ["connection"] = new Dictionary<object, object>
            {
                ["id"] = context.Connection.Id,
                ["remote_address"] = context.Connection.RemoteIpAddress.ToString()
            }
        };
    }
}