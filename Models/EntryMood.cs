using SQLite;

namespace Journal.Models
{
    public class EntryMood
    {
        [PrimaryKey, AutoIncrement]
        public int EntryMoodId { get; set; }

        public int EntryId { get; set; }   // FK
        public int MoodId { get; set; }    // FK

        public string MoodType { get; set; } = "Primary"; // Primary / Secondary
    }
}
