using System.ComponentModel.DataAnnotations;
using SQLite;

namespace Journal.Models
{
    public class Mood
    {
        [PrimaryKey, AutoIncrement]
        public int MoodId { get; set; }

        [Required(ErrorMessage = "Mood name is required")]
        public string MoodName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mood category is required")]
        public string MoodCategory { get; set; } = string.Empty;
    }
}
