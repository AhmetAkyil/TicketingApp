using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;

namespace TicketSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class KanbanController : Controller
    {
        private readonly AppDbContext _context;
        public KanbanController(AppDbContext context) => _context = context;

        private long? GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(idStr, out var uid) ? uid : (long?)null;
        }

        // GET /Kanban/Pins 
        [HttpGet]
        public async Task<IActionResult> Pins()
        {
            var uid = GetCurrentUserId();
            if (uid == null) return Challenge();

            var ids = await _context.KanbanPins
                .AsNoTracking()
                .Where(p => p.UserId == uid)
                .Select(p => p.TicketId)
                .ToListAsync();

            return Json(ids);
        }

        // POST /Kanban/Add  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long ticketId)
        {
            var uid = GetCurrentUserId();
            if (uid == null) return Challenge();

            var exists = await _context.KanbanPins.AnyAsync(p => p.UserId == uid && p.TicketId == ticketId);
            if (!exists)
            {
                _context.KanbanPins.Add(new Models.KanbanPin { UserId = uid.Value, TicketId = ticketId });
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST /Kanban/Remove 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(long ticketId)
        {
            var uid = GetCurrentUserId();
            if (uid == null) return Challenge();

            var pin = await _context.KanbanPins.FirstOrDefaultAsync(p => p.UserId == uid && p.TicketId == ticketId);
            if (pin != null)
            {
                _context.KanbanPins.Remove(pin);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST /Kanban/Save  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] long[] ticketIds)
        {
            var uid = GetCurrentUserId();
            if (uid == null) return Challenge();

            var current = await _context.KanbanPins.Where(p => p.UserId == uid).ToListAsync();

            _context.KanbanPins.RemoveRange(current.Where(p => !ticketIds.Contains(p.TicketId)));

            var toAdd = ticketIds.Distinct().Except(current.Select(p => p.TicketId)).ToList();
            foreach (var tid in toAdd)
                _context.KanbanPins.Add(new Models.KanbanPin { UserId = uid.Value, TicketId = tid });

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
