using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.Authorization
{
    public class AuthorizationService
    {
        private readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();
        private readonly RepositoryService _repositoryService;

        public AuthorizationService(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        public async Task<DeviceAuthorizationContext> AuthorizeDeviceAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.IdentityUid, out var identityUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.ChannelUid, out var channelUidHeaderValue);
            httpContext.Request.Headers.TryGetValue(CustomCloudHeaderNames.Password, out var password);

            var identityUid = identityUidHeaderValue.ToString().ToLowerInvariant();
            var channelUid = channelUidHeaderValue.ToString().ToLowerInvariant();

            await AuthorizeAsync(identityUid, password).ConfigureAwait(false);
            return new DeviceAuthorizationContext(identityUid, channelUid);
        }

        public async Task<List<Claim>> AuthorizeAsync(string identityUid, string password)
        {
            if (string.IsNullOrEmpty(identityUid) || string.IsNullOrEmpty(password))
            {
                throw new UnauthorizedAccessException();
            }

            identityUid = identityUid.ToLowerInvariant();

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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, identityUid)
            };

            if (identityConfiguration.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
            }

            return claims;
        }

        public Task SetPasswordAsync(string identityUid, string newPassword)
        {
            return _repositoryService.SetPasswordAsync(identityUid, newPassword);
        }
    }
}
