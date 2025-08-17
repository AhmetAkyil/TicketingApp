using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;
using TicketSystem.Services;

namespace TicketSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.AsNoTracking()
                                           .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create() => View();

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            _context.Add(user);
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Kayıt başarısız. Email zaten kullanılıyor olabilir.");
                return View(user);
            }
        }

        // POST: users/create-auto  (admin otomatik kullanıcı üretimi)
        [HttpPost]
        [Route("users/create-auto")]
        [IgnoreAntiforgeryToken] 
        [AllowAnonymous]
        public async Task<IActionResult> CreateAuto(string firstName, string lastName, string role)
        {
            var generator = new AccountCreationService();
            var email = generator.GenerateEmail(firstName, lastName);
            var password = generator.GeneratePassword();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return BadRequest("Bu e-posta zaten kullanılıyor.");
            }

            if (!Enum.TryParse<UserRoles>(role, true, out var parsedRole))
            {
                return BadRequest("Geçersiz rol.");
            }

            var user = new User
            {
                Email = email,
                Password = password,
                Role = parsedRole
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Kullanıcı başarıyla oluşturuldu.",
                Email = email,
                TemporaryPassword = password
            });
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("UserId,Email,Role")] User user)
        {
            if (id != user.UserId) return NotFound();
            if (!ModelState.IsValid) return View(user);

            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Email = user.Email;
            entity.Role = user.Role;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Güncelleme başarısız. Email zaten kullanılıyor olabilir.");
                return View(user);
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.AsNoTracking()
                                           .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
