using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Enums;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CommentsController : Controller
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long ticketId, string commentText)
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            var t = await _context.Tickets.AsNoTracking()
                .Where(x => x.TicketId == ticketId)
                .Select(x => new { x.TicketId, x.CreatedByUserId, x.AssignedToUserId })
                .FirstOrDefaultAsync();

            if (t == null) return NotFound();

            var isAdmin = me.Value.Role == UserRoles.Admin;
            var isCreator = t.CreatedByUserId == me.Value.UserId;
            var assignedToMe = t.AssignedToUserId != null && t.AssignedToUserId == me.Value.UserId;

            if (!isAdmin && !isCreator && !assignedToMe)
                return Forbid();

            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction("Details", "Tickets", new { id = ticketId });
            }

            commentText = commentText.Trim();
            if (commentText.Length > 2000)
                commentText = commentText.Substring(0, 2000);

            var comment = new Comment
            {
                TicketId = t.TicketId,
                userId = me.Value.UserId,
                commentText = commentText,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Tickets", new { id = ticketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            var comment = await _context.Comments
                .Include(c => c.Ticket)
                .FirstOrDefaultAsync(c => c.commentId == id);

            if (comment == null) return NotFound();

            var isAdmin = me.Value.Role == UserRoles.Admin;
            var isOwner = comment.userId == me.Value.UserId;
            var isAssignedToMe = comment.Ticket?.AssignedToUserId != null
                                 && comment.Ticket.AssignedToUserId == me.Value.UserId;

            if (!isAdmin && !isOwner && !isAssignedToMe)
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }

        // GET: Comments/Edit/id
        public async Task<IActionResult> Edit(long id)
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            var comment = await _context.Comments
                .Include(c => c.Ticket)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.commentId == id);

            if (comment == null)
                return NotFound();

            // Sadece sahibi düzenleyebilir
            if (comment.userId != me.Value.UserId)
                return Forbid();

            return View(comment);
        }

        // POST: Comments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Comment updatedComment)
        {
            var me = GetCurrent();
            if (me == null) return Challenge();

            var comment = await _context.Comments.FindAsync(updatedComment.commentId);
            if (comment == null)
                return NotFound();

            if (comment.userId != me.Value.UserId)
                return Forbid();

            comment.commentText = updatedComment.commentText;
            comment.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
        }
    }
}
