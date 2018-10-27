using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Wirehome.Core.Cloud.Messages;
using Wirehome.Cloud.Services.Repository;

namespace Wirehome.Cloud.Services
{
    public class AuthorizationService
    {
        private readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();
        private readonly RepositoryService _repositoryService;

        public AuthorizationService(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        public AuthorizationScope Authorize(AuthorizeCloudMessage authorizeMessage)
        {
            if (authorizeMessage == null) throw new ArgumentNullException(nameof(authorizeMessage));

            if (string.IsNullOrEmpty(authorizeMessage.IdentityUid) ||
                string.IsNullOrEmpty(authorizeMessage.Password) ||
                string.IsNullOrEmpty(authorizeMessage.ChannelUid))
            {
                throw new UnauthorizedAccessException();
            }

            var identity = authorizeMessage.IdentityUid.ToLowerInvariant();

            var identityEntity = _repositoryService.GetIdentities()
                .FirstOrDefault(i => string.Equals(i.Uid, identity, StringComparison.Ordinal));

            if (identityEntity == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (_passwordHasher.VerifyHashedPassword(identity, identityEntity.PasswordHash, authorizeMessage.Password) != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException();
            }

            var channelEntity = identityEntity.Channels
                .FirstOrDefault(c => string.Equals(c.Uid, authorizeMessage.ChannelUid, StringComparison.Ordinal));

            if (channelEntity == null)
            {
                throw new UnauthorizedAccessException();
            }

            return new AuthorizationScope(identity, authorizeMessage.ChannelUid);
        }
    }

    public class AuthorizationScope
    {
        public AuthorizationScope(string identityUid, string channelUid)
        {
            IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
            ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));
        }

        public string IdentityUid { get; }

        public string ChannelUid { get; }

        public override string ToString()
        {
            return $"{IdentityUid}/{ChannelUid}";
        }

        public override int GetHashCode()
        {
            return IdentityUid.GetHashCode(StringComparison.Ordinal) ^ ChannelUid.GetHashCode(StringComparison.Ordinal);
        }
    }
}
