#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using CoAPnet;
using CoAPnet.Client;
using CoAPnet.Extensions.DTLS;
using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Wirehome.Core.Python;

namespace Wirehome.Core.Hardware.CoAP
{
    public class CoapServicePythonProxy : IInjectedPythonProxy
    {
        readonly Dictionary<string, ICoapClient> _clients = new Dictionary<string, ICoapClient>();

        readonly CoapService _coapService;

        public string ModuleName => "coap";

        public CoapServicePythonProxy(CoapService coapService)
        {
            _coapService = coapService ?? throw new ArgumentNullException(nameof(coapService));
        }

        public PythonDictionary request(PythonDictionary parameters)
        {
            if (parameters is null) throw new ArgumentNullException(nameof(parameters));

            var clientUid = Convert.ToString(parameters.get("client_uid", string.Empty));
            var protocol = Convert.ToString(parameters.get("protocol", "dtls"));
            var host = Convert.ToString(parameters.get("host", string.Empty));
            var port = Convert.ToInt32(parameters.get("port", 5684));
            var method = Convert.ToString(parameters.get("method", "get"));
            var path = Convert.ToString(parameters.get("path", string.Empty));
            var payload = Convert.ToString(parameters.get("payload", string.Empty));
            var identity = Convert.ToString(parameters.get("identity", string.Empty));
            var key = Convert.ToString(parameters.get("key", string.Empty));

            var connectOptionsBuilder = new CoapClientConnectOptionsBuilder()
                .WithHost(host)
                .WithPort(port);

            if (protocol == "dtls")
            {
                connectOptionsBuilder.WithDtlsTransportLayer(new DtlsCoapTransportLayerOptionsBuilder().WithPreSharedKey(identity, key).Build());
            }

            var connectOptions = connectOptionsBuilder.Build();

            var request = new CoapRequestBuilder()
                .WithMethod((CoapRequestMethod)Enum.Parse(typeof(CoapRequestMethod), method, true))
                .WithPath(path)
                .WithPayload(payload)
                .Build();

            CoapResponse response;
            if (!string.IsNullOrEmpty(clientUid))
            {
                ICoapClient coapClient;
                lock (_clients)
                {
                    if (!_clients.TryGetValue(clientUid, out coapClient))
                    {
                        coapClient = new CoapFactory().CreateClient();
                        coapClient.ConnectAsync(connectOptions, CancellationToken.None).GetAwaiter().GetResult();
                        _clients[clientUid] = coapClient;
                    }
                }

                try
                {
                    response = coapClient.RequestAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch
                {
                    coapClient.Dispose();

                    lock (_clients)
                    {
                        _clients.Remove(clientUid);
                    }

                    return new PythonDictionary
                    {
                        ["type"] = "exception"
                    };
                }
            }
            else
            {
                using (var coapClient = new CoapFactory().CreateClient())
                {
                    coapClient.ConnectAsync(connectOptions, CancellationToken.None).GetAwaiter().GetResult();
                    response = coapClient.RequestAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            var responsePayload = string.Empty;
            if (response.Payload.Array != null)
            {
                responsePayload = Encoding.UTF8.GetString(response.Payload);
            }

            return new PythonDictionary
            {
                ["type"] = "success",
                ["status"] = response.StatusCode.ToString(),
                ["status_code"] = (int)response.StatusCode,
                ["payload"] = responsePayload
            };
        }
    }
}
