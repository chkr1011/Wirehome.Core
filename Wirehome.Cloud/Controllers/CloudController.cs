using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Controllers
{
    [Authorize]
    [ApiController]
    public class CloudController : Controller
    {
        private readonly DeviceConnectorService _deviceConnectorService;
        private readonly AuthorizationService _authorizationService;

        public CloudController(DeviceConnectorService deviceConnectorService, AuthorizationService authorizationService)
        {
            _deviceConnectorService = deviceConnectorService ?? throw new ArgumentNullException(nameof(deviceConnectorService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
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

        [HttpPost]
        [Route("/api/v1/cloud/ping")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task GetPing()
        {
            var request = new CloudMessage
            {
                Type = CloudMessageType.Ping
            };

            var deviceSessionIdentifier = await _authorizationService.GetDeviceSessionIdentifier(HttpContext).ConfigureAwait(false);
            await _deviceConnectorService.Invoke(deviceSessionIdentifier, request, HttpContext.RequestAborted).ConfigureAwait(false);
        }

        [HttpPost]
        [Route("/api/v1/cloud/invoke")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult> PostInvoke([FromBody] JToken content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var request = new CloudMessage
            {
                Type = CloudMessageType.Raw,
                Payload = Encoding.UTF8.GetBytes(content.ToString())
            };

            var deviceSessionIdentifier = await _authorizationService.GetDeviceSessionIdentifier(HttpContext).ConfigureAwait(false);
            var responseMessage = await _deviceConnectorService.Invoke(deviceSessionIdentifier, request, HttpContext.RequestAborted).ConfigureAwait(false);

            return new ContentResult
            {
                Content = Encoding.UTF8.GetString(responseMessage.Payload),
                ContentType = "application/json"
            };
        }
    }
}
