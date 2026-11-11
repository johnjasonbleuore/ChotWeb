using System.Threading.RateLimiting;
using ChotWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---- Storage (App_Data): content JSON, uploads, sign-in keys ----
StoragePaths paths = new(builder.Environment.ContentRootPath, builder.Configuration["Storage:DataPath"]);
builder.Services.AddSingleton(paths);
builder.Services.AddSingleton<SiteContentStore>();
builder.Services.AddSingleton<MediaStore>();

// ---- Admin sign-in by emailed one-time code (allowed emails: Admin:AllowedEmails) ----
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AdminAllowList>();
builder.Services.AddSingleton<LoginCodeStore>();
builder.Services.AddSingleton<LoginCodeMailer>();
builder.Services.AddHttpClient(nameof(LoginCodeMailer), client => client.Timeout = TimeSpan.FromSeconds(20));

builder.Services.AddControllersWithViews(options =>
{
    // Blank text (e.g. an empty follower count) is allowed; required fields use [Required] explicitly.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Keeps admins signed in across restarts (keys live in App_Data/keys).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(paths.KeysDir))
    .SetApplicationName("ChotWeb");

// ---- Admin sign-in ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "ChotWeb.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = AdminAllowList.ValidatePrincipalAsync;
        options.Events.OnRedirectToLogin = context =>
        {
            // The designer's API calls get a 401 instead of an HTML redirect.
            if (context.Request.Path.StartsWithSegments("/admin/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }

            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// At most 5 sign-in attempts (code requests + code entries) per minute from one address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Too many attempts. Please wait a minute and try again.", token);
    };
});

WebApplication app = builder.Build();

// Point out missing admin settings at startup.
int adminCount = app.Services.GetRequiredService<AdminAllowList>().Emails.Count;
if (adminCount == 0)
{
    app.Logger.LogWarning("No admin emails are set, so nobody can sign in to /admin. Set Admin__AllowedEmails (comma-separated) in the environment.");
}
else
{
    app.Logger.LogInformation("{Count} email(s) can sign in to /admin.", adminCount);
}

if (!app.Services.GetRequiredService<LoginCodeMailer>().IsConfigured)
{
    app.Logger.LogWarning("Email sending isn't set up (Email__BrevoApiKey or Email__Smtp__Host, plus Email__From). Sign-in codes will be written to this log instead.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    // Only this site may show its pages in a frame (the designer preview).
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
    if (context.Request.Path.StartsWithSegments("/admin"))
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    await next();
});

// Uploaded photos/videos (App_Data/uploads) at /uploads/… — supports video seeking (range requests).
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(paths.UploadsDir),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
    },
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
