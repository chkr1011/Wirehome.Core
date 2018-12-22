using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.Connector;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Controllers
{
    public class FunctionsController : Controller
    {
        private readonly ConnectorService _connectorService;
        private readonly AuthorizationService _authorizationService;

        public FunctionsController(ConnectorService connectorService, AuthorizationService authorizationService)
        {
            _connectorService = connectorService ?? throw new ArgumentNullException(nameof(connectorService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        [HttpGet]
        [Route("/api/v1/cloud/ping")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<JToken> GetPing()
        {
            var authorizationContext = _authorizationService.AuthorizeHttpCall(HttpContext);

            var request = new CloudMessage
            {
                Type = CloudMessageType.Ping
            };

            var response = await _connectorService.Invoke(authorizationContext, request, HttpContext.RequestAborted).ConfigureAwait(false);
            return response.Content;
        }

        [HttpPost]
        [Route("/api/v1/cloud/invoke")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<JToken> PostInvoke([FromBody] JToken content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var authorizationContext = _authorizationService.AuthorizeHttpCall(HttpContext);

            var request = new CloudMessage
            {
                Type = CloudMessageType.Raw,
                Content = content
            };

            var response = await _connectorService.Invoke(authorizationContext, request, HttpContext.RequestAborted).ConfigureAwait(false);
            return response.Content;
        }

        [HttpGet]
        [Route("/api/v1/cloud/passwords/{password}/hash")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetPasswordHash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var passwordHasher = new PasswordHasher<string>();
            return passwordHasher.HashPassword(string.Empty, password);
        }
    }
}
