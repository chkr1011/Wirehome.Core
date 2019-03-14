using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Wirehome.Cloud.Services.Repository;

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

            var routeTemplate = TemplateParser.Parse("{identityUid}/Channels/{channelUid}");
            var values = new RouteValueDictionary();
            var templateMatcher = new TemplateMatcher(routeTemplate, values);
            var isMatch = templateMatcher.TryMatch(httpContext.Request.Path, values);

            if (!isMatch || !httpContext.Request.Query.ContainsKey("password"))
            {
                throw new UnauthorizedAccessException();
            }

            var identityUid = Convert.ToString(values["identityUid"]);
            var channelUid = Convert.ToString(values["channelUid"]);
            var passwordHash = httpContext.Request.Query["password"];

            await AuthorizeAsync(identityUid, passwordHash).ConfigureAwait(false);
            return new DeviceAuthorizationContext(identityUid.ToLowerInvariant(), channelUid);
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
