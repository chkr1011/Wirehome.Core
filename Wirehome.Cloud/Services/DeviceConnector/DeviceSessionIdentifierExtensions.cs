using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public static class DeviceSessionIdentifierExtensions
    {
        public static DeviceSessionIdentifier GetDeviceSessionIdentifier(this HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            
            if (httpContext.User == null)
            {
                return null;
            }

            var identityUid = httpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(identityUid))
            {
                return null;
            }

            httpContext.Request.Cookies.TryGetValue(CookieNames.ChannelUid, out var channelUid);
            if (string.IsNullOrEmpty(channelUid))
            {
                channelUid = "default";
            }

            return new DeviceSessionIdentifier(identityUid, channelUid);
        }
    }
}
