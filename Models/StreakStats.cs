namespace Journal.Models
{
    public class StreakStats
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public List<DateTime> MissedDays { get; set; } = new();
    }
}