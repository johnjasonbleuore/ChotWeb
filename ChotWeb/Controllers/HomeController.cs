using System.Diagnostics;
using ChotWeb.Models;
using ChotWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChotWeb.Controllers
{
    /// <summary>The public site visitors land on.</summary>
    public class HomeController(SiteContentStore store) : Controller
    {
        /// <param name="preview">Signed-in admins can add ?preview=true to see the unpublished draft.</param>
        public IActionResult Index(bool preview = false)
        {
            bool isPreview = preview && User.Identity?.IsAuthenticated == true;
            SiteContent site = isPreview ? store.GetDraft() : store.GetLive();

            ViewData["Site"] = site;
            ViewData["Preview"] = isPreview;
            if (isPreview)
            {
                Response.Headers.CacheControl = "no-store";
            }

            return View(site);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
