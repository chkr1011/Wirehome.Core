using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Wirehome.Core.Cloud.Protocol.Content;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessageFactory
    {
        private readonly CloudMessageSerializer _cloudMessageSerializer;

        public CloudMessageFactory(CloudMessageSerializer cloudMessageSerializer)
        {
            _cloudMessageSerializer = cloudMessageSerializer ?? throw new ArgumentNullException(nameof(cloudMessageSerializer));
        }

        public CloudMessage Create(Exception exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));

            return new CloudMessage
            {
                Type = CloudMessageType.Error,
                Payload = _cloudMessageSerializer.Pack(new ExceptionCloudMessageContent
                {
                    Exception = exception.ToString()
                })
            };
        }

        public async Task<CloudMessage> Create(HttpRequest httpRequest)
        {
            if (httpRequest is null) throw new ArgumentNullException(nameof(httpRequest));

            var cloudMessageContent = new HttpRequestCloudMessageContent
            {
                Method = httpRequest.Method,
                Uri = httpRequest.Path + httpRequest.QueryString,
                Content = await LoadHttpRequestContent(httpRequest).ConfigureAwait(false)
            };

            if (!string.IsNullOrEmpty(httpRequest.ContentType))
            {
                cloudMessageContent.Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = httpRequest.ContentType
                };
            }

            var requestMessage = new CloudMessage
            {
                Type = CloudMessageType.HttpInvoke,
                Payload = _cloudMessageSerializer.Pack(cloudMessageContent)
            };

            return requestMessage;
        }

        public async Task<CloudMessage> Create(HttpResponseMessage httpResponse, string correlationId)
        {
            var responseContent = new HttpResponseCloudMessageContent();

            if ((int)httpResponse.StatusCode != 200)
            {
                responseContent.StatusCode = (int)httpResponse.StatusCode;
            }

            var responseBody = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            if (responseBody.Any())
            {
                responseContent.Content = responseBody;
            }

            if (httpResponse.Content.Headers.ContentType != null)
            {
                responseContent.Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = httpResponse.Content.Headers.ContentType.ToString()
                };
            }

            return new CloudMessage
            {
                CorrelationId = correlationId,
                Payload = _cloudMessageSerializer.Pack(responseContent)
            };
        }

        async Task<byte[]> LoadHttpRequestContent(HttpRequest httpRequest)
        {
            if (!httpRequest.ContentLength.HasValue)
                return null;

            var buffer = new byte[httpRequest.ContentLength.Value];
            await httpRequest.Body.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return buffer;
        }
    }
}
