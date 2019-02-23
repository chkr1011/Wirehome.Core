using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Controllers
{
    [Authorize]
    [ApiController]
    public class CloudController : Controller
    {
        private readonly DeviceConnectorService _deviceConnectorService;
        
        public CloudController(DeviceConnectorService deviceConnectorService)
        {
            _deviceConnectorService = deviceConnectorService ?? throw new ArgumentNullException(nameof(deviceConnectorService));
        }
        
        [HttpGet]
        [Route("api/v1/cloud/statistics")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IActionResult GetStatistics()
        {
            return new ObjectResult(_deviceConnectorService.GetStatistics());
        }

        [HttpGet]
        [Route("api/v1/cloud/statistics/{identityUid}/{channelUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IActionResult GetChannelStatistics(string identityUid, string channelUid = "default")
        {
            var deviceSessionIdentifier = new DeviceSessionIdentifier(identityUid, channelUid);
            return new ObjectResult(_deviceConnectorService.GetChannelStatistics(deviceSessionIdentifier));
        }

        [HttpDelete]
        [Route("api/v1/cloud/statistics/{identityUid}/{channelUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteChannelStatistics(string identityUid, string channelUid = "default")
        {
            var deviceSessionIdentifier = new DeviceSessionIdentifier(identityUid, channelUid);
            _deviceConnectorService.ResetChannelStatistics(deviceSessionIdentifier);
        }

        [HttpGet]
        [Route("/api/v1/cloud/ping")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<JToken> GetPing()
        {
            var request = new CloudMessage
            {
                Type = CloudMessageType.Ping
            };

            var deviceSessionIdentifier = HttpContext.GetDeviceSessionIdentifier();
            var response = await _deviceConnectorService.Invoke(deviceSessionIdentifier, request, HttpContext.RequestAborted).ConfigureAwait(false);
            return response.Content;
        }

        [HttpPost]
        [Route("/api/v1/cloud/invoke")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<JToken> PostInvoke([FromBody] JToken content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var request = new CloudMessage
            {
                Type = CloudMessageType.Raw,
                Content = content
            };

            var deviceSessionIdentifier = HttpContext.GetDeviceSessionIdentifier();
            var response = await _deviceConnectorService.Invoke(deviceSessionIdentifier, request, HttpContext.RequestAborted).ConfigureAwait(false);
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
