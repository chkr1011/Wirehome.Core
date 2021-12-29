#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoAPnet;
using CoAPnet.Client;
using CoAPnet.Extensions.DTLS;
using IronPython.Runtime;
using Wirehome.Core.Constants;
using Wirehome.Core.Python;

namespace Wirehome.Core.Hardware.CoAP
{
    public sealed class CoapServicePythonProxy : IInjectedPythonProxy
    {
        readonly Dictionary<string, ICoapClient> _clients = new Dictionary<string, ICoapClient>();
        readonly CoapFactory _coapFactory = new CoapFactory();

        public string ModuleName => "coap";

        public PythonDictionary request(PythonDictionary parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            try
            {
                var coapRequest = CreateRequest(parameters);
                var coapResponse = ExecuteCoapRequest(coapRequest, parameters);

                return new PythonDictionary
                {
                    ["type"] = WirehomeMessageType.Success,
                    ["status"] = coapResponse.StatusCode.ToString(),
                    ["status_code"] = (int) coapResponse.StatusCode,
                    ["payload"] = new Bytes(coapResponse.Payload ?? Array.Empty<byte>()),
                    ["payload_string"] = Encoding.UTF8.GetString(coapResponse.Payload ?? Array.Empty<byte>())
                };
            }
            catch (Exception exception)
            {
                return new PythonDictionary
                {
                    ["type"] = WirehomeMessageType.Exception,
                    ["message"] = exception.Message
                };
            }
        }

        async Task<ICoapClient> CreateClient(PythonDictionary parameters)
        {
            var host = Convert.ToString(parameters.get("host", string.Empty));
            var port = Convert.ToInt32(parameters.get("port", 5684));
            var protocol = Convert.ToString(parameters.get("protocol", "dtls"));
            var identity = Convert.ToString(parameters.get("identity", string.Empty));
            var key = Convert.ToString(parameters.get("key", string.Empty));
            var timeout = Convert.ToInt32(parameters.get("timeout", 1000));

            var connectOptionsBuilder = new CoapClientConnectOptionsBuilder().WithHost(host).WithPort(port);

            if (protocol == "dtls")
            {
                connectOptionsBuilder.WithDtlsTransportLayer(o => o.WithPreSharedKey(identity, key));
            }

            var connectOptions = connectOptionsBuilder.Build();

            var coapClient = _coapFactory.CreateClient();
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await coapClient.ConnectAsync(connectOptions, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch
            {
                coapClient?.Dispose();
                throw;
            }

            return coapClient;
        }

        static CoapRequest CreateRequest(PythonDictionary parameters)
        {
            var method = Convert.ToString(parameters.get("method", "get"));
            var path = Convert.ToString(parameters.get("path", string.Empty));
            var payload = parameters.get("payload", Array.Empty<byte>());

            return new CoapRequestBuilder().WithMethod((CoapRequestMethod) Enum.Parse(typeof(CoapRequestMethod), method, true)).WithPath(path)
                .WithPayload(PythonConvert.ToPayload(payload)).Build();
        }

        CoapResponse ExecuteCoapRequest(CoapRequest coapRequest, PythonDictionary parameters)
        {
            var clientUid = Convert.ToString(parameters.get("client_uid", null));
            var timeout = Convert.ToInt32(parameters.get("timeout", 1000));

            if (!string.IsNullOrEmpty(clientUid))
            {
                ICoapClient coapClient;
                lock (_clients)
                {
                    if (!_clients.TryGetValue(clientUid, out coapClient))
                    {
                        coapClient = CreateClient(parameters).GetAwaiter().GetResult();
                        _clients[clientUid] = coapClient;
                    }
                }

                try
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                    {
                        lock (coapClient)
                        {
                            return coapClient.RequestAsync(coapRequest, cancellationTokenSource.Token).GetAwaiter().GetResult();
                        }
                    }
                }
                catch
                {
                    coapClient.Dispose();

                    lock (_clients)
                    {
                        _clients.Remove(clientUid);
                    }

                    throw;
                }
            }

            CoapResponse coapResponse;
            using (var coapClient = CreateClient(parameters).GetAwaiter().GetResult())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    coapResponse = coapClient.RequestAsync(coapRequest, cancellationTokenSource.Token).GetAwaiter().GetResult();
                }
            }

            return coapResponse;
        }
    }
}