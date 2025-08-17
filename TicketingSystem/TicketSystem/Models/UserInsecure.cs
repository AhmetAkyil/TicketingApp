using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class UserInsecure
    {
        [Key]
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
