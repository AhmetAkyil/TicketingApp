using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using TicketSystem.Enums;

namespace TicketSystem.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }  

        [Required]
        public string Password { get; set; }

        [Required]
        public UserRoles Role { get; set; }

        [ValidateNever]
        public ICollection<Ticket> CreatedTickets { get; set; }

        [ValidateNever]
        public ICollection<Ticket> AssignedTickets { get; set; }

    }
}
