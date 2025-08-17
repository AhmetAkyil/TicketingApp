using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Services;

namespace TicketSystem.Controllers
{
    [ApiController]
    [Route("auth")]
    
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRecaptchaService _recaptcha;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IRecaptchaService recaptcha, IConfiguration config)
        {
            _context = context;
            _recaptcha = recaptcha;
            _config = config;
        }

        [HttpGet("login")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult LoginForm()
        {
            ViewBag.SiteKey = _config["GoogleReCaptcha:SiteKey"];
            return View("Login"); 
        }

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm(Name = "g-recaptcha-response")] string recaptchaToken,
            [FromQuery] string? returnUrl = null)
        {
            ViewBag.SiteKey = _config["GoogleReCaptcha:SiteKey"];

            // reCAPTCHA
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var captchaOk = await _recaptcha.VerifyAsync(recaptchaToken, remoteIp);
            if (!captchaOk)
            {
                ViewData["Error"] = "Lütfen robot doğrulamasını tamamlayın.";
                return View("Login");
            }

            Console.WriteLine($"Login denemesi: {email} / {password}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            
            await SignInWithCookieAsync(user.UserId, user.Email, user.Role.ToString());

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // DEMO: ratelimiter yok
        [HttpPost("login-open")]
        [IgnoreAntiforgeryToken]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> LoginOpen([FromForm] string email, [FromForm] string password)
        {
            Console.WriteLine($"[LoginOpen] Deneme: {email} / {password}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            await SignInWithCookieAsync(user.UserId, user.Email, user.Role.ToString());
            return RedirectToAction("Index", "Home");
        }

        // DEMO: SQL Injection gösterimi 
        [HttpPost("login-insecure")]
        [IgnoreAntiforgeryToken] // demo için
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> LoginInsecure([FromForm] string email, [FromForm] string password)
        {
            var sql = $"SELECT * FROM Users WHERE Email = '{email}' AND Password = '{password}'";
            var userList = await _context.Users.FromSqlRaw(sql).ToListAsync();
            var user = userList.OrderByDescending(x => x.UserId).FirstOrDefault();

            if (user == null)
            {
                ViewData["Error"] = "Hatalı kullanıcı adı veya şifre.";
                return View("Login");
            }

            await SignInWithCookieAsync(user.UserId, user.Email, user.Role.ToString());
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginForm", "Auth");
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied() => View("AccessDenied");



        private async Task SignInWithCookieAsync(long userId, string email, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }
    }
}
