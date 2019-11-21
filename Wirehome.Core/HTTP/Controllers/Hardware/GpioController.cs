using Microsoft.AspNetCore.Mvc;
using System;
using Wirehome.Core.Hardware.GPIO;

namespace Wirehome.Core.HTTP.Controllers.Hardware
{
    [ApiController]
    public class GpioController : Controller
    {
        private readonly GpioRegistryService _gpioService;

        public GpioController(GpioRegistryService gpioService)
        {
            _gpioService = gpioService ?? throw new ArgumentNullException(nameof(gpioService));
        }

        [HttpGet]
        [Route("/api/v1/gpios/{hostId}/{id}/state")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetGpioState(string hostId, int id)
        {
            return _gpioService.ReadState(hostId, id).ToString().ToLowerInvariant();
        }

        [HttpGet]
        [Route("/api/v1/gpios/{id}/state")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetGpioState(int id)
        {
            return _gpioService.ReadState(string.Empty, id).ToString().ToLowerInvariant();
        }

        [HttpPost]
        [Route("/api/v1/gpios/{hostId}/{id}/state")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostGpioState(string hostId, int id, [FromBody] string state)
        {
            _gpioService.WriteState(hostId, id, Enum.Parse<GpioState>(state, true));
        }

        [HttpPost]
        [Route("/api/v1/gpios/{id}/state")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostGpioState(int id, [FromBody] string state)
        {
            _gpioService.WriteState(string.Empty, id, Enum.Parse<GpioState>(state, true));
        }

        [HttpPost]
        [Route("/api/v1/gpios/{id}/set_direction")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostGpioDirection(int id, string direction)
        {
            var directionValue = Enum.Parse<GpioDirection>(direction, true);

            _gpioService.SetDirection(string.Empty, id, directionValue);
        }
    }
}
