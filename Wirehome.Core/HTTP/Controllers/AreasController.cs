using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Areas;

namespace Wirehome.Core.HTTP.Controllers
{
    public class AreasController : Controller
    {
        private readonly AreaRegistryService _areaRegistryService;

        public AreasController(AreaRegistryService areaRegistryService)
        {
            _areaRegistryService = areaRegistryService ?? throw new ArgumentNullException(nameof(areaRegistryService));
        }

        [HttpGet]
        [Route("api/v1/areas")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Area> GetAreas()
        {
            return _areaRegistryService.GetAreas();
        }

        [HttpGet]
        [Route("api/v1/areas/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Area GetArea(string uid)
        {
            return _areaRegistryService.GetArea(uid);
        }

        [HttpPost]
        [Route("api/v1/areas/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostArea(string uid, [FromBody] AreaConfiguration configuration)
        {
            _areaRegistryService.InitializeArea(uid, configuration);
        }

        [HttpPost]
        [Route("api/v1/areas/{areaUid}/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostAreaComponent(string areaUid, string componentUid)
        {
            if (areaUid == null) throw new ArgumentNullException(nameof(areaUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            if (!_areaRegistryService.TryGetArea(areaUid, out var area))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            area.Components.Add(componentUid);
            _areaRegistryService.Save();
        }

        [HttpDelete]
        [Route("api/v1/areas/{areaUid}/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteAreaComponent(string areaUid, string componentUid)
        {
            if (areaUid == null) throw new ArgumentNullException(nameof(areaUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            if (!_areaRegistryService.TryGetArea(areaUid, out var area))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            area.Components.Remove(componentUid);
            _areaRegistryService.Save();
        }

        [HttpPost]
        [Route("/api/v1/areas/{areaUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostAreaSetting(string areaUid, string settingUid, [FromBody] object value)
        {
            _areaRegistryService.SetAreaSetting(areaUid, settingUid, value);
        }

        [HttpGet]
        [Route("/api/v1/areas/{areaUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetAreaSetting(string areaUid, string settingUid)
        {
            return _areaRegistryService.GetAreaSetting(areaUid, settingUid);
        }
    }
}
