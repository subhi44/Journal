using Journal.Models;

namespace Journal.Services
{
    public class AnalyticsService : DatabaseService
    {
        private bool _initialized;

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.ExecuteAsync("PRAGMA foreign_keys = ON;");

            // Ensure tables exist (safe)
            await _db.CreateTableAsync<User>();
            await _db.CreateTableAsync<JournalEntry>();
            await _db.CreateTableAsync<EntryMood>();
            await _db.CreateTableAsync<EntryTag>();
            await _db.CreateTableAsync<Mood>();
            await _db.CreateTableAsync<Tag>();

            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        public async Task<AnalyticsResult> GetAnalyticsAsync(
            int userId,
            DateTime fromUtcDate,
            DateTime toUtcDate,
            JournalEntryService journalEntryService)
        {
            await EnsureInitializedAsync();

            var from = fromUtcDate.Date;
            var to = toUtcDate.Date;
            if (to < from) (from, to) = (to, from);

            // Entries in range
            var entries = await _db.Table<JournalEntry>()
                .Where(e => e.UserId == userId && e.EntryDate >= from && e.EntryDate <= to)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            var result = new AnalyticsResult
            {
                FromDate = from,
                ToDate = to,
                TotalEntries = entries.Count
            };

            // ---- Streak (global) - reuse your existing logic
            var streak = await journalEntryService.GetStreakStatsAsync(userId);
            result.CurrentStreak = streak.CurrentStreak;
            result.LongestStreak = streak.LongestStreak;

            // Missed days in selected range
            result.MissedDaysInRange = CalculateMissedDaysInRange(
                entries.Select(e => e.EntryDate.Date).Distinct().ToList(),
                from,
                to
            );

            if (entries.Count == 0)
            {
                // still return streak + missed days etc.
                result.MoodDistribution = new Dictionary<string, int>
                {
                    ["Positive"] = 0,
                    ["Neutral"] = 0,
                    ["Negative"] = 0
                };
                return result;
            }

            var entryIds = entries.Select(e => e.EntryId).ToList();

            // Lookup tables
            var moods = await _db.Table<Mood>().ToListAsync();
            var tags = await _db.Table<Tag>().ToListAsync();
            var moodById = moods.ToDictionary(m => m.MoodId, m => m);
            var tagById = tags.ToDictionary(t => t.TagId, t => t);

            // Relations
            var entryMoods = await _db.Table<EntryMood>()
                .Where(em => entryIds.Contains(em.EntryId))
                .ToListAsync();

            var entryTags = await _db.Table<EntryTag>()
                .Where(et => entryIds.Contains(et.EntryId))
                .ToListAsync();

            // =========================
            // Mood Distribution (PRIMARY category)
            // =========================
            var primary = entryMoods
                .Where(x => x.MoodType == "Primary")
                .ToList();

            int pos = 0, neu = 0, neg = 0;
            foreach (var p in primary)
            {
                if (!moodById.TryGetValue(p.MoodId, out var m)) continue;

                var categoryName = m.MoodCategory == "Category"
                    ? m.MoodName
                    : m.MoodCategory;

                if (categoryName == "Positive") pos++;
                else if (categoryName == "Neutral") neu++;
                else if (categoryName == "Negative") neg++;
            }

            result.MoodDistribution = new Dictionary<string, int>
            {
                ["Positive"] = pos,
                ["Neutral"] = neu,
                ["Negative"] = neg
            };

            // =========================
            // Most Frequent Mood (SECONDARY mood name)
            // =========================
            var secondary = entryMoods
                .Where(x => x.MoodType == "Secondary")
                .ToList();

            if (secondary.Count > 0)
            {
                var topSecondary = secondary
                    .GroupBy(x => x.MoodId)
                    .OrderByDescending(g => g.Count())
                    .First();

                result.MostFrequentMood = moodById.TryGetValue(topSecondary.Key, out var m)
                    ? m.MoodName
                    : $"Mood {topSecondary.Key}";
            }

            // =========================
            // Most Used Tags (counts)
            // =========================
            var tagCounts = entryTags
                .GroupBy(t => t.TagId)
                .Select(g =>
                {
                    var name = tagById.TryGetValue(g.Key, out var t) ? t.Tagname : $"Tag {g.Key}";
                    return new TagCount { TagId = g.Key, TagName = name, Count = g.Count() };
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.TagName)
                .ToList();

            result.MostUsedTags = tagCounts.Take(10).ToList();

            // =========================
            // Tag Breakdown (% of entries that used each tag)
            // =========================
            var entriesPerTag = entryTags
                .GroupBy(et => et.TagId)
                .Select(g => new
                {
                    TagId = g.Key,
                    EntryCount = g.Select(x => x.EntryId).Distinct().Count()
                })
                .ToList();

            result.TagBreakdown = entriesPerTag
                .Select(x =>
                {
                    var name = tagById.TryGetValue(x.TagId, out var t) ? t.Tagname : $"Tag {x.TagId}";
                    var pct = result.TotalEntries == 0 ? 0 : (x.EntryCount * 100.0 / result.TotalEntries);

                    return new TagPercent
                    {
                        TagId = x.TagId,
                        TagName = name,
                        EntriesCount = x.EntryCount,
                        Percent = Math.Round(pct, 1)
                    };
                })
                .OrderByDescending(x => x.Percent)
                .ThenBy(x => x.TagName)
                .Take(12)
                .ToList();

            // =========================
            // Word Count Trends (avg words per day)
            // =========================
            result.WordCountTrends = entries
                .OrderBy(e => e.EntryDate)
                .Select(e => new WordTrendPoint
                {
                    Date = e.EntryDate.Date,
                    AvgWords = e.WordCount
                })
                .ToList();

            return result;
        }

        private static List<DateTime> CalculateMissedDaysInRange(List<DateTime> entryDates, DateTime start, DateTime end)
        {
            var set = new HashSet<DateTime>(entryDates.Select(d => d.Date));
            var missed = new List<DateTime>();

            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                if (!set.Contains(d))
                    missed.Add(d);
            }

            return missed;
        }
    }
}
