using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Cloud.Controllers.Models;
using Wirehome.Cloud.Services.Authorization;

namespace Wirehome.Cloud.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    readonly AuthorizationService _authorizationService;

    public AccountController(AuthorizationService authorizationService)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    [Route("cloud/account/channel/{channelUid}/token")]
    [HttpGet]
    public async Task<IActionResult> GetToken(string channelUid, string refreshToken = null)
    {
        string identityUid;
        if (HttpContext.User != null)
        {
            identityUid = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        }
        else
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            var identityEntity = await _authorizationService.FindIdentityUidByChannelAccessToken(refreshToken).ConfigureAwait(false);
            if (identityEntity.Key == null)
            {
                throw new UnauthorizedAccessException();
            }

            identityUid = identityEntity.Key;

            throw new NotImplementedException();
        }

        var newAccessToken = await _authorizationService.UpdateChannelAccessToken(identityUid, channelUid).ConfigureAwait(false);

        return new ObjectResult(newAccessToken);
    }

    [Route("cloud/account")]
    [Route("cloud/account/index")]
    [HttpGet]
    public IActionResult Index()
    {
        return View(nameof(Index), new LoginModel());
    }

    [Route("cloud/account/login")]
    [HttpGet]
    public IActionResult Login(string returnUrl)
    {
        return View(nameof(Index), new LoginModel
        {
            ReturnUrl = returnUrl
        });
    }

    [Route("cloud/account/login")]
    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        try
        {
            await _authorizationService.AuthorizeUser(HttpContext, model.IdentityUid, model.Password).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            // TODO: Use in Razor!
            ModelState.TryAddModelError("LoginError", "UNAUTHORIZED");
            return View(nameof(Index), model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect(nameof(Index));
    }

    [Route("cloud/account/logout")]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

        return Redirect(nameof(Index));
    }

    [Route("cloud/account/password")]
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SetPassword(string newPassword)
    {
        if (string.IsNullOrEmpty(newPassword))
        {
            throw new InvalidOperationException();
        }

        await _authorizationService.UpdatePassword(User.Identity.Name, newPassword).ConfigureAwait(false);
        return await Logout().ConfigureAwait(false);
    }
}