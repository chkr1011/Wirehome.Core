using Microsoft.AspNetCore.Mvc;
using System;
using Wirehome.Core.Hardware.I2C;

namespace Wirehome.Core.HTTP.Controllers.Hardware
{
    [ApiController]
    public class I2CController : Controller
    {
        readonly I2CBusService _i2CBusService;

        public I2CController(I2CBusService i2cBusService)
        {
            _i2CBusService = i2cBusService ?? throw new ArgumentNullException(nameof(i2cBusService));
        }

        [HttpPost]
        [Route("/api/v1/i2c_bus/{hostId}/devices/{deviceAddress}/buffer")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostBuffer(string hostId, int deviceAddress, [FromBody] byte[] buffer)
        {
            _i2CBusService.Write(hostId, deviceAddress, new ArraySegment<byte>(buffer));
        }

        [HttpPost]
        [Route("/api/v1/i2c_bus/devices/{deviceAddress}/buffer")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostBuffer(int deviceAddress, [FromBody] byte[] buffer)
        {
            _i2CBusService.Write(string.Empty, deviceAddress, new ArraySegment<byte>(buffer));
        }

        [HttpGet]
        [Route("/api/v1/i2c_bus/{hostId}/devices/{deviceAddress}/buffer")]
        [ApiExplorerSettings(GroupName = "v1")]
        public byte[] GetBuffer(string hostId, int deviceAddress, int length)
        {
            var buffer = new byte[length];
            _i2CBusService.Read(hostId, deviceAddress, new ArraySegment<byte>(buffer));
            return buffer;
        }

        [HttpGet]
        [Route("/api/v1/i2c_bus/devices/{deviceAddress}/buffer")]
        [ApiExplorerSettings(GroupName = "v1")]
        public byte[] GetBuffer(int deviceAddress, int length)
        {
            var buffer = new byte[length];
            _i2CBusService.Read(string.Empty, deviceAddress, new ArraySegment<byte>(buffer));
            return buffer;
        }
    }
}
