using CoAPnet.Client;
using System.Threading;

namespace Wirehome.Core.Hardware.CoAP
{
    public class CoapClientInstance
    {
        readonly ICoapClient _coapClient;

        public CoapClientInstance(ICoapClient coapClient)
        {
            _coapClient = coapClient ?? throw new global::System.ArgumentNullException(nameof(coapClient));
        }

    }
}
