using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Cloud;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class CloudController : Controller
{
    readonly CloudService _cloudService;

    public CloudController(CloudService cloudService)
    {
        _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
    }
}