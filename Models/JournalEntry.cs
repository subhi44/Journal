using System.ComponentModel.DataAnnotations;
using SQLite;

namespace Journal.Models
{
    public class JournalEntry
    {
        [PrimaryKey, AutoIncrement]
        public int EntryId { get; set; }

        [Required]
        public int UserId { get; set; }   // FK

        [Required, StringLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime EntryDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // READ-ONLY (calculated)
        public int WordCount =>
            string.IsNullOrWhiteSpace(Content)
                ? 0
                : Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
