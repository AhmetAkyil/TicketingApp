using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
    public class Comment
    {
        public long commentId { get; set; }

        [Required]
        public string commentText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public long TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket Ticket { get; set; }

        public long userId { get; set; }
        [ForeignKey("userId")]
        public User User { get; set; }
    }
}
