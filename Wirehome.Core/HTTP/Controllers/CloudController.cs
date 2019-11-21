using Microsoft.AspNetCore.Mvc;
using System;
using Wirehome.Core.Cloud;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class CloudController : Controller
    {
        private readonly CloudService _cloudService;

        public CloudController(CloudService cloudService)
        {
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
        }

        [HttpPost]
        [Route("api/v1/cloud/reconnect")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostReconnect()
        {
            _cloudService.Reconnect();
        }
    }
}
