using ChotWeb.Models;
using ChotWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChotWeb.Controllers
{
    /// <summary>
    /// Admin area pages: sign-in, designer and media library.
    /// Usernames and passwords come from the "Admin:Users" setting (see <see cref="AdminUsers"/>).
    /// </summary>
    [Authorize]
    [Route("admin")]
    [AutoValidateAntiforgeryToken]
    public class AdminController(AdminUsers admins, ILogger<AdminController> logger) : Controller
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

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? username = admins.Verify(model.Username, model.Password);
            if (username is null)
            {
                IReadOnlyDictionary<string, string> users = admins.Users;
                logger.LogWarning(
                    "Failed admin sign-in for '{Username}' from {Ip}. {Count} user(s) are set up: {Usernames}",
                    model.Username, HttpContext.Connection.RemoteIpAddress, users.Count, string.Join(", ", users.Keys));
                ModelState.AddModelError("", users.Count == 0
                    ? "No admin accounts are set up on this server yet. Add Admin__Users in the hosting settings."
                    : "Wrong username or password.");
                model.Password = "";
                return View(model);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                admins.CreatePrincipal(username),
                new AuthenticationProperties { IsPersistent = model.RememberMe });
            logger.LogInformation("{Username} signed in to the admin.", username);

            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(Designer));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
