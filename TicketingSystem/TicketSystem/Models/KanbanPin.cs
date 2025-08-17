using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
    public class KanbanPin
    {
        [Key]
        public long KanbanPinId { get; set; }

        [ForeignKey(nameof(User))]
        public long UserId { get; set; }

        [ForeignKey(nameof(Ticket))]
        public long TicketId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Ticket Ticket { get; set; } = null!;
    }
}
