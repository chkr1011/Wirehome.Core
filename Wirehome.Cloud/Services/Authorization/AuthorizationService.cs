using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.Authorization
{
    public class AuthorizationService
    {
        private readonly RepositoryService _repositoryService;
        readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();

        public AuthorizationService(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        public async Task<DeviceAuthorizationContext> AuthorizeDevice(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.IdentityUid, out var identityUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.ChannelUid, out var channelUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.AccessToken, out var accessToken);

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
        }

        public Task SetPasswordAsync(string identityUid, string newPassword)
        {
            return _repositoryService.SetPasswordAsync(identityUid, newPassword);
        }
    }
}
