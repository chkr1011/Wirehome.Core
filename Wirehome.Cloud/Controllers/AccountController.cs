using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Wirehome.Cloud.Services.Authorization;

namespace Wirehome.Cloud.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AuthorizationService _authorizationService;

        public AccountController(AuthorizationService authorizationService)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(nameof(Index), new LoginModel());
        }

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View(nameof(Index), new LoginModel {ReturnUrl = returnUrl});
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

            return Redirect(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var claims = await _authorizationService.AuthorizeAsync(model.IdentityUid, model.Password).ConfigureAwait(false);

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authenticationProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authenticationProperties);
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetPassword(string newPassword)
        {
            await _authorizationService.SetPasswordAsync(User.Identity.Name, newPassword).ConfigureAwait(false);
            return await Logout();
        }
    }
}
