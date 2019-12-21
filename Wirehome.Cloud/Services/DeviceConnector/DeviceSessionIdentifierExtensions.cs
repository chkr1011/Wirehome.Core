using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using Wirehome.Core.Cloud.Protocol;

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

            if (!httpContext.Request.Cookies.TryGetValue(CloudCookieNames.ChannelUid, out var channelUid))
            {
                return null;
            }

            if (string.IsNullOrEmpty(channelUid))
            {
                return null;
            }

            // The identity can be either the current one as default or a dedicated one specified in the channel.
            // user1@cloud.de/channelX
            if (channelUid.Contains("/"))
            {
                var fragments = channelUid.Split("/");
                if (fragments.Length != 2)
                {
                    throw new InvalidOperationException("Channel UID is invalid.");
                }

                return new DeviceSessionIdentifier(fragments[0], fragments[1]);
                // TODO: Check if target user granted access!
            }

            // Use the identity from the currently authenticated user.
            var identityUid = httpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(identityUid))
            {
                return null;
            }

            return new DeviceSessionIdentifier(identityUid, channelUid);
        }
    }
}
