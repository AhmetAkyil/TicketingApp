using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketSystem.Enums;

namespace TicketSystem.Models
{
    public class Ticket
    {
        [Key]
        public long TicketId { get; set; }
        
        [Required]
        public string Title { get; set; }   
        public string Description { get; set; }
        [Required]
        public TicketStatus Status { get; set; }


        public DateTime CreatedDate { get; set; }

        [ForeignKey("CreatedByUser")]
        public long CreatedByUserId { get; set; }
        [ValidateNever]
        public User CreatedByUser { get; set; }


        [ForeignKey("AssignedToUser")]
        public long? AssignedToUserId { get; set; }
        [ValidateNever]
        public User AssignedToUser { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    }
}
