using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

        [Route("Cloud/Account")]
        [HttpGet]
        public IActionResult Index()
        {
            return View(nameof(Index), new LoginModel());
        }

        [Route("Cloud/Account/Login")]
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View(nameof(Index), new LoginModel {ReturnUrl = returnUrl});
        }

        [Route("Cloud/Account/Logout")]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

            return Redirect(nameof(Index));
        }

        [Route("Cloud/Account/Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                await _authorizationService.Authorize(HttpContext, model.IdentityUid, model.Password).ConfigureAwait(false);
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
