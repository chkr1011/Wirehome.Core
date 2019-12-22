using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.Authorization
{
    public class AuthorizationService
    {
        readonly RepositoryService _repositoryService;
        readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();

        public AuthorizationService(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        public async Task<DeviceAuthorizationContext> AuthorizeDevice(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            httpContext.Request.Headers.TryGetValue(CloudHeaderNames.IdentityUid, out var identityUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CloudHeaderNames.ChannelUid, out var channelUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CloudHeaderNames.AccessToken, out var accessToken);

            var identityUid = identityUidHeaderValue.ToString().ToLowerInvariant();
            var channelUid = channelUidHeaderValue.ToString().ToLowerInvariant();

            var identityConfiguration = await _repositoryService.TryGetIdentityConfigurationAsync(identityUid).ConfigureAwait(false);
            if (identityConfiguration == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (identityConfiguration.IsLocked)
            {
                throw new UnauthorizedAccessException();
            }

            if (!identityConfiguration.DeviceAccessTokens.TryGetValue(accessToken, out var accessTokenConfiguration))
            {
                throw new UnauthorizedAccessException();
            }

            return new DeviceAuthorizationContext(identityUid, channelUid);
        }

        public async Task AuthorizeUser(HttpContext httpContext, string identityUid, string password)
        {
            var identityConfiguration = await _repositoryService.TryGetIdentityConfigurationAsync(identityUid).ConfigureAwait(false);
            if (identityConfiguration == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (identityConfiguration.IsLocked)
            {
                throw new UnauthorizedAccessException();
            }

            if (_passwordHasher.VerifyHashedPassword(identityUid, identityConfiguration.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException();
            }

            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, identityUid));
            if (identityConfiguration.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authenticationProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authenticationProperties).ConfigureAwait(false);

            var defaultChannel = identityConfiguration.Channels.FirstOrDefault(c => c.Value.IsDefault);
            if (defaultChannel.Key == null)
            {
                defaultChannel = identityConfiguration.Channels.FirstOrDefault();
            }

            httpContext.Response.Cookies.Append(CloudCookieNames.ChannelUid, defaultChannel.Key);
        }

        public Task SetPasswordAsync(string identityUid, string newPassword)
        {
            return _repositoryService.SetPasswordAsync(identityUid, newPassword);
        }

        public async Task<DeviceSessionIdentifier> GetDeviceSessionIdentifier(HttpContext httpContext)
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
                await Task.CompletedTask;
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
