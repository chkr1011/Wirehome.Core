using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Cloud.Services.Repository.Entities;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.Authorization;

public class AuthorizationService
{
    readonly PasswordHasher<string> _passwordHasher = new();
    readonly RepositoryService _repositoryService;

    public AuthorizationService(RepositoryService repositoryService)
    {
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
    }

    public async Task<ChannelIdentifier> AuthorizeDevice(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (!httpContext.Request.Headers.TryGetValue(CloudHeaderNames.ChannelAccessToken, out var channelAccessToken))
        {
            throw new UnauthorizedAccessException();
        }

        var identityEntity = await _repositoryService.FindIdentityEntityByChannelAccessToken(channelAccessToken).ConfigureAwait(false);
        if (identityEntity.Key == null)
        {
            throw new UnauthorizedAccessException();
        }

        if (identityEntity.Value.IsLocked)
        {
            throw new UnauthorizedAccessException();
        }

        var channelEntity = identityEntity.Value.Channels.First(c => string.Equals(c.Value.AccessToken.Value, channelAccessToken, StringComparison.Ordinal));
        if (channelEntity.Key == null)
        {
            throw new UnauthorizedAccessException();
        }

        return new ChannelIdentifier(identityEntity.Key, channelEntity.Key);
    }

    public async Task AuthorizeUser(HttpContext httpContext, string identityUid, string password)
    {
        var identityConfiguration = await _repositoryService.TryGetIdentityEntityAsync(identityUid).ConfigureAwait(false);
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
            new(ClaimTypes.Name, identityUid)
        };

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

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authenticationProperties).ConfigureAwait(false);

        var defaultChannel = identityConfiguration.Channels.FirstOrDefault(c => c.Value.IsDefault);
        if (defaultChannel.Key == null)
        {
            defaultChannel = identityConfiguration.Channels.FirstOrDefault();
        }

        httpContext.Response.Cookies.Append(CloudCookieNames.ChannelUid, defaultChannel.Key, new CookieOptions
        {
            IsEssential = true
        });
    }

    public Task<KeyValuePair<string, IdentityEntity>> FindIdentityUidByChannelAccessToken(string channelAccessToken)
    {
        if (channelAccessToken is null)
        {
            throw new ArgumentNullException(nameof(channelAccessToken));
        }

        return _repositoryService.FindIdentityEntityByChannelAccessToken(channelAccessToken);
    }

    public async Task<ChannelIdentifier> GetChannelIdentifier(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (httpContext.User == null)
        {
            return null;
        }

        var identityUid = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(identityUid))
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

            var owningIdentityUid = fragments[0];
            channelUid = fragments[1];

            var owningIdentity = await _repositoryService.TryGetIdentityEntityAsync(owningIdentityUid).ConfigureAwait(false);
            if (owningIdentity == null)
            {
                return null;
            }

            if (!owningIdentity.Channels.TryGetValue(channelUid, out var owningChannel))
            {
                return null;
            }

            if (!owningChannel.AllowedIdentities.TryGetValue(identityUid, out var allowedIdentityEntity))
            {
                return null;
            }

            return new ChannelIdentifier(owningIdentityUid, channelUid);
        }

        return new ChannelIdentifier(identityUid, channelUid);
    }

    public async Task<AccessTokenEntity> UpdateChannelAccessToken(string identityUid, string channelUid)
    {
        if (identityUid is null)
        {
            throw new ArgumentNullException(nameof(identityUid));
        }

        if (channelUid is null)
        {
            throw new ArgumentNullException(nameof(channelUid));
        }

        var accessTokenEntity = new AccessTokenEntity
        {
            Value = GenerateAccessToken(),
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        await _repositoryService.UpdateIdentity(identityUid, e =>
        {
            if (!e.Channels.TryGetValue(channelUid, out var channel))
            {
                channel = new ChannelEntity();
                e.Channels[channelUid] = channel;
            }

            channel.AccessToken = accessTokenEntity;
        }).ConfigureAwait(false);

        return accessTokenEntity;
    }

    public Task UpdatePassword(string identityUid, string newPassword)
    {
        if (identityUid is null)
        {
            throw new ArgumentNullException(nameof(identityUid));
        }

        if (newPassword is null)
        {
            throw new ArgumentNullException(nameof(newPassword));
        }

        return _repositoryService.UpdateIdentity(identityUid, e =>
        {
            var passwordHasher = new PasswordHasher<string>();
            e.PasswordHash = passwordHasher.HashPassword(identityUid, newPassword);
        });
    }

    static string GenerateAccessToken()
    {
        var buffer = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(buffer);
    }
}