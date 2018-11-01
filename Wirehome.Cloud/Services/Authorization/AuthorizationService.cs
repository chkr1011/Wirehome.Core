using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud.Messages;

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

        public AuthorizationContext AuthorizeBasic(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationValues))
            {
                httpContext.Response.Headers.TryAdd("WWW-Authenticate", "Basic");
                throw new UnauthorizedAccessException();
            }

            var authorizationValue = authorizationValues.FirstOr(string.Empty);
            if (!authorizationValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException();
            }

            authorizationValue = authorizationValue.Substring("Basic ".Length);
            authorizationValue = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationValue));

            var identityUid = authorizationValue.Substring(0, authorizationValue.IndexOf(":", StringComparison.Ordinal));
            identityUid = identityUid.ToLowerInvariant();
            var password = authorizationValue.Substring(identityUid.Length + 1);
            
            var channelUid = "default";
            if (httpContext.Request.Headers.TryGetValue("Wirehome-ChannelUid", out var channelUidValues))
            {
                channelUid = channelUidValues.ToString();
            }

            return Authorize(identityUid, password, channelUid);
        }

        public AuthorizationContext AuthorizeConnector(AuthorizeContent authorizeContent)
        {
            if (authorizeContent == null) throw new ArgumentNullException(nameof(authorizeContent));

            if (string.IsNullOrEmpty(authorizeContent.IdentityUid) ||
                string.IsNullOrEmpty(authorizeContent.Password) ||
                string.IsNullOrEmpty(authorizeContent.ChannelUid))
            {
                throw new UnauthorizedAccessException();
            }

            return Authorize(authorizeContent.IdentityUid, authorizeContent.Password, authorizeContent.ChannelUid);
        }

        private AuthorizationContext Authorize(string identityUid, string password, string channelUid)
        {
            identityUid = identityUid.ToLowerInvariant();

            if (!_repositoryService.TryGetIdentityConfiguration(identityUid, out var identityConfiguration))
            {
                throw new UnauthorizedAccessException();
            }

            if (identityConfiguration.IsLocked)
            {
                throw new UnauthorizedAccessException();
            }

            if (!identityConfiguration.Channels.TryGetValue(channelUid, out _))
            {
                throw new UnauthorizedAccessException();
            }

            if (_passwordHasher.VerifyHashedPassword(identityUid, identityConfiguration.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException();
            }

            return new AuthorizationContext(identityUid, channelUid);
        }
    }
}
