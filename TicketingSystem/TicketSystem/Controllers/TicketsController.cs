using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        private (long UserId, UserRoles Role)? GetCurrent()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false)) return null;

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleStr = User.FindFirstValue(ClaimTypes.Role);

            if (!long.TryParse(idStr, out var uid)) return null;
            if (!Enum.TryParse<UserRoles>(roleStr, out var role)) role = UserRoles.Customer;

            return (uid, role);
        }

        private bool CurrentIsAdmin()
        {
            var roleStr = User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(roleStr, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        // GET: Tickets
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .ToListAsync();

            return View(tickets);
        }

        // GET: Tickets/Details/id
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var me = GetCurrent();
            if (me == null) return Challenge();

            bool isAdmin = me.Value.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.Value.UserId || t.AssignedToUserId == me.Value.UserId))
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();
            return View(ticket);
        }

        // GET: Tickets/Create
        public async Task<IActionResult> Create()
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            ViewBag.Users = new SelectList(
                await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                nameof(TicketSystem.Models.User.UserId),
                nameof(TicketSystem.Models.User.Email)
            );

            return View(new Ticket { Status = TicketStatus.Open });
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,AssignedToUserId")] Ticket ticket)
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            if (ticket.AssignedToUserId.HasValue)
            {
                bool assigneeExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == ticket.AssignedToUserId.Value);
                if (!assigneeExists)
                    ModelState.AddModelError(nameof(ticket.AssignedToUserId), "Atanacak kullanıcı bulunamadı.");
            }

            ticket.CreatedByUserId = me.Value.UserId;
            ticket.CreatedDate = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(
                    await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                    nameof(TicketSystem.Models.User.UserId),
                    nameof(TicketSystem.Models.User.Email),
                    ticket.AssignedToUserId
                );
                return View(ticket);
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Tickets/Edit/ticketId
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var me = GetCurrent();
            if (me == null) return Challenge();

            bool isAdmin = me.Value.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.Value.UserId))
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();

            ViewBag.Users = new SelectList(
                await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                nameof(TicketSystem.Models.User.UserId),
                nameof(TicketSystem.Models.User.Email),
                ticket.AssignedToUserId
            );

            return View(ticket);
        }

        // POST: Tickets/Edit/ticketId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("TicketId,Title,Description,Status,AssignedToUserId")] Ticket form)
        {
            if (id != form.TicketId) return NotFound();

            var me = GetCurrent();
            if (me == null) return Challenge();

            bool isAdmin = me.Value.Role == UserRoles.Admin;

            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id &&
                            (isAdmin || t.CreatedByUserId == me.Value.UserId))
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();

            if (form.AssignedToUserId.HasValue)
            {
                bool assignedExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == form.AssignedToUserId.Value);
                if (!assignedExists)
                    ModelState.AddModelError(nameof(form.AssignedToUserId), "Atanacak kullanıcı bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(
                    await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync(),
                    nameof(TicketSystem.Models.User.UserId),
                    nameof(TicketSystem.Models.User.Email),
                    form.AssignedToUserId
                );
                return View(form);
            }

            ticket.Title = form.Title;
            ticket.Description = form.Description;
            ticket.Status = form.Status;
            ticket.AssignedToUserId = form.AssignedToUserId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Home");
        }

        // GET: Tickets/Delete/ticketId
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.TicketId == id);
            if (ticket == null) return NotFound();

            var me = GetCurrent();
            if (me == null) return Challenge();

            var isAdmin = me.Value.Role == UserRoles.Admin;
            if (!isAdmin && ticket.CreatedByUserId != me.Value.UserId)
                return Forbid();

            return View(ticket);
        }

        // POST: Tickets/Delete/ticketId
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var me = GetCurrent();
            if (me == null) return Challenge();

            var isAdmin = me.Value.Role == UserRoles.Admin;
            if (!isAdmin && ticket.CreatedByUserId != me.Value.UserId)
                return Forbid();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Home");
        }

        public async Task<IActionResult> MyTickets()
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            var myTickets = await _context.Tickets
                .Where(t => t.CreatedByUserId == me.Value.UserId)
                .ToListAsync();

            return View(myTickets);
        }
    }
}
