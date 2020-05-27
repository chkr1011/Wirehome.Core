using Microsoft.AspNetCore.Mvc;
using System;
using Wirehome.Core.Cloud;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class CloudController : Controller
    {
        readonly CloudService _cloudService;

        public CloudController(CloudService cloudService)
        {
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
        }
    }
}
