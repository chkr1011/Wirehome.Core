using System;
using Microsoft.AspNetCore.Mvc;
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
        public string GetPing(string identityUid, string channelUid)
        {
            return "";
        }
    }
}
