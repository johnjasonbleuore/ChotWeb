using ChotWeb.Models;
using ChotWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChotWeb.Controllers
{
    /// <summary>Admin area pages: sign-in, first-run setup, designer, media library and account.</summary>
    [Authorize]
    [Route("admin")]
    [AutoValidateAntiforgeryToken]
    public class AdminController(AdminAccountStore accounts, ILogger<AdminController> logger) : Controller
    {
        [HttpGet("")]
        public IActionResult Designer()
        {
            return View();
        }

        [HttpGet("media")]
        public IActionResult Media()
        {
            return View();
        }

        [HttpGet("account")]
        public IActionResult Account()
        {
            return View(new AccountViewModel { Username = accounts.Username ?? "" });
        }

        [HttpPost("account")]
        public async Task<IActionResult> Account(AccountViewModel model)
        {
            if (ModelState.IsValid && !accounts.Verify(accounts.Username, model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Your current password is wrong.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            accounts.Update(model.Username, model.NewPassword);
            await SignInAsync(persistent: false);
            logger.LogInformation("Admin account details were changed.");

            TempData["Message"] = string.IsNullOrEmpty(model.NewPassword) ? "Username saved." : "Username and password saved.";
            return RedirectToAction(nameof(Account));
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (!accounts.HasAccount)
            {
                return RedirectToAction(nameof(Setup));
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Designer));
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!accounts.HasAccount)
            {
                return RedirectToAction(nameof(Setup));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!accounts.Verify(model.Username, model.Password))
            {
                logger.LogWarning("Failed admin sign-in for '{Username}' from {Ip}.", model.Username, HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError("", "Wrong username or password.");
                model.Password = "";
                return View(model);
            }

            await SignInAsync(model.RememberMe);
            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(Designer));
        }

        [AllowAnonymous]
        [HttpGet("setup")]
        public IActionResult Setup()
        {
            if (accounts.HasAccount)
            {
                return RedirectToAction(nameof(Login));
            }

            accounts.GetOrCreateSetupCode();
            return View(new SetupViewModel());
        }

        [AllowAnonymous]
        [HttpPost("setup")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Setup(SetupViewModel model)
        {
            if (accounts.HasAccount)
            {
                return RedirectToAction(nameof(Login));
            }

            if (ModelState.IsValid && !accounts.CheckSetupCode(model.SetupCode))
            {
                ModelState.AddModelError(nameof(model.SetupCode), "That setup code is wrong.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            accounts.Create(model.Username, model.Password);
            logger.LogInformation("Admin account '{Username}' created.", model.Username);
            await SignInAsync(persistent: false);
            return RedirectToAction(nameof(Designer));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private Task SignInAsync(bool persistent)
        {
            return HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                accounts.CreatePrincipal(),
                new AuthenticationProperties { IsPersistent = persistent });
        }
    }
}
