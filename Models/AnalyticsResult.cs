namespace Journal.Models
{
    public class AnalyticsResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int TotalEntries { get; set; }

        // Mood distribution (Primary category) e.g. Positive/Neutral/Negative
        public Dictionary<string, int> MoodDistribution { get; set; } = new();

        // Most frequent mood (Secondary mood name)
        public string MostFrequentMood { get; set; } = "—";

        // Streaks are global (not date-range); you already calculate these
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        // Missed days within selected range (based on entries in that range)
        public List<DateTime> MissedDaysInRange { get; set; } = new();

        // Tag usage (based on EntryTag in range)
        public List<TagCount> MostUsedTags { get; set; } = new();

        // % of entries per tag (in range)
        public List<TagPercent> TagBreakdown { get; set; } = new();

        // Avg words per day (in range) - since one entry/day, avg == that day's wordcount
        public List<WordTrendPoint> WordCountTrends { get; set; } = new();
    }

    public class TagCount
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = "";
        public int Count { get; set; }
    }

    public class TagPercent
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = "";
        public int EntriesCount { get; set; }
        public double Percent { get; set; } // 0..100
    }

    public class WordTrendPoint
    {
        public DateTime Date { get; set; }
        public int AvgWords { get; set; }
    }
}
