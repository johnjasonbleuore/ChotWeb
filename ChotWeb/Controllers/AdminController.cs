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
    /// Admin area pages: email sign-in, designer and media library.
    /// Only emails on the allow list (Admin:AllowedEmails) can sign in; each sign-in uses a one-time emailed code.
    /// </summary>
    [Authorize]
    [Route("admin")]
    [AutoValidateAntiforgeryToken]
    public class AdminController(
        AdminAllowList allowList,
        LoginCodeStore codes,
        LoginCodeMailer mailer,
        ILogger<AdminController> logger) : Controller
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

        // ---- Step 1: email ----

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null, string? email = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Designer));
            }

            return View(new LoginEmailViewModel { ReturnUrl = returnUrl, Email = email ?? "" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginEmailViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = AdminAllowList.Normalize(model.Email);

            // People not on the list see the same "check your email" page, so the list can't be guessed.
            if (!allowList.IsAllowed(email))
            {
                logger.LogWarning("Sign-in code requested for {Email}, which is not on the admin list ({Ip}).", email, HttpContext.Connection.RemoteIpAddress);
                return RedirectToCodePage(email, model.ReturnUrl);
            }

            if (codes.TryIssue(email, out string code, out _))
            {
                try
                {
                    await mailer.SendAsync(email, code, cancellationToken);
                }
                catch (Exception ex)
                {
                    codes.Cancel(email);
                    logger.LogError(ex, "Couldn't email the sign-in code to {Email}.", email);
                    ModelState.AddModelError("", "We couldn't send the email just now. Please try again in a minute.");
                    return View(model);
                }
            }

            return RedirectToCodePage(email, model.ReturnUrl);
        }

        // ---- Step 2: code ----

        [AllowAnonymous]
        [HttpGet("login/code")]
        public IActionResult Code(string? email, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new LoginCodeViewModel { Email = AdminAllowList.Normalize(email), ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost("login/code")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Code(LoginCodeViewModel model)
        {
            model.Email = AdminAllowList.Normalize(model.Email);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Checking the list again here means an email removed after its code was sent can't get in.
            if (!allowList.IsAllowed(model.Email) || !codes.Verify(model.Email, model.Code))
            {
                logger.LogWarning("Wrong or expired sign-in code for {Email} ({Ip}).", model.Email, HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(nameof(model.Code), "That code is wrong or has expired. Check the latest email, or send a new code.");
                model.Code = "";
                return View(model);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                AdminAllowList.CreatePrincipal(model.Email),
                new AuthenticationProperties { IsPersistent = model.RememberMe });
            logger.LogInformation("{Email} signed in to the admin.", model.Email);

            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(Designer));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private RedirectToActionResult RedirectToCodePage(string email, string? returnUrl)
        {
            return RedirectToAction(nameof(Code), new { email, returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
        }
    }
}
