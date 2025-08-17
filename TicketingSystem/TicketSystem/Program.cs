using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;
using TicketSystem.Data;
using TicketSystem.Services;

var builder = WebApplication.CreateBuilder(args);


// RateLimiter
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        string retryInfo = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? $"Retry after {retryAfter} seconds"
            : "no retry info";

        Console.WriteLine("RATE LIMIT BLOCKED for key: " + retryInfo);

        var response = context.HttpContext.Response;
        response.StatusCode = 503;
        response.ContentType = "text/html";

        await response.WriteAsync(@"
        <html>
            <head><title>Too many attempts</title></head>
            <body style='font-family: Arial; text-align: center; padding-top: 100px;'>
                <h2>503 - Limit Exceeded</h2>
                <p>Please try again in 1 minute.</p>
                <a href='/Auth/Login'>Return to login page</a>
            </body>
        </html>
    ");
    };

    options.AddPolicy("LoginPolicy", context =>
    {
        var ip = context.Connection.RemoteIpAddress;
        var ipKey = ip?.MapToIPv4().ToString() ?? "unknown";

        
        var email = context.Request.HasFormContentType
            ? context.Request.Form["email"].ToString().ToLowerInvariant()
            : null;

        Console.WriteLine($"[RATE] PartitionKey (IP): ip:{ipKey}");
        if (!string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine($"[RATE] Login denemesi yapılan e-posta: {email}");
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ipKey}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Alternatif (email/IP karma) :
    /*
    options.AddPolicy("LoginPolicy", context =>
    {
        var email = context.Request.HasFormContentType
            ? context.Request.Form["email"].ToString().ToLowerInvariant()
            : null;

        var ip = context.Connection.RemoteIpAddress;
        string ipString = ip?.MapToIPv4().ToString() ?? "unknown";

        var partitionKey = !string.IsNullOrWhiteSpace(email)
            ? $"login:{email}"
            : $"ip:{ipString}";

        Console.WriteLine($"[RATE] LoginPolicy partitionKey: {partitionKey}");
        Console.WriteLine($"[RATE] IP Address: {ipString}");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
    */
});


// MVC + Global Anti-Forgery
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});


// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Recaptcha & HttpClient
builder.Services.Configure<RecaptchaOptions>(
    builder.Configuration.GetSection("GoogleReCaptcha"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();


// Authentication / Authorization (Cookie + Claims)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".TicketSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.ReturnUrlParameter = "returnUrl";
    });

builder.Services.AddAuthorization(options =>
{
    // Varsayılan olarak her endpoint authentication istesin
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});


// Build
var app = builder.Build();


// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();



app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication(); 
app.UseAuthorization();


app.MapControllers();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
