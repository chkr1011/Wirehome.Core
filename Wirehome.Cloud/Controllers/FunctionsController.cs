using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Cloud.Services;

namespace Wirehome.Cloud.Controllers
{
    public class FunctionsController : Controller
    {
        private readonly ConnectorService _connectorService;

        public FunctionsController(ConnectorService connectorService)
        {
            _connectorService = connectorService ?? throw new ArgumentNullException(nameof(connectorService));
        }

        [HttpGet]
        [Route("/api/v1/identities/{identityUid}/channels/{channelUid}/ping")]
        public Task<JObject> GetPing(string identityUid, string channelUid)
        {
            var message = new JObject { ["type"] = "wirehome.cloud.message.ping" };

            return _connectorService.Invoke(
                identityUid,
                channelUid,
                message,
                HttpContext.RequestAborted);
        }
    }
}
