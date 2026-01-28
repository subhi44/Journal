using SQLite;
using System.ComponentModel.DataAnnotations;

namespace Journal.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int UserId { get; set; }
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 15 characters")]
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }


    }
}
