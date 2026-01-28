using Journal.Models;

namespace Journal.Services
{
    public class JournalEntryService : DatabaseService
    {
        private bool _initialized;

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await _db.CreateTableAsync<JournalEntry>();
            await _db.CreateTableAsync<EntryMood>();
            await _db.CreateTableAsync<EntryTag>();

            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        // ======================= Journal Entries =======================

        // Get entries for a specific user
        public async Task<List<JournalEntry>> GetEntriesAsync(int userId)
        {
            await EnsureInitializedAsync();

            return await _db.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        // Get all entries
        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            await EnsureInitializedAsync();

            return await _db.Table<JournalEntry>()
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        public async Task<int> SaveEntryAsync(JournalEntry entry)
        {
            await EnsureInitializedAsync();

            if (entry.UserId <= 0)
                throw new Exception("User is required.");

            // INSERT (new entry)
            if (entry.EntryId == 0)
            {
                // Force system date ONLY on insert
                entry.EntryDate = DateTime.UtcNow.Date;

                // One entry per user per day (insert check)
                var existing = await _db.Table<JournalEntry>()
                    .Where(e => e.UserId == entry.UserId && e.EntryDate == entry.EntryDate)
                    .FirstOrDefaultAsync();

                if (existing != null)
                    throw new Exception("Only one journal entry per day is allowed.");

                entry.CreatedAt = DateTime.UtcNow;
                entry.UpdatedAt = DateTime.UtcNow;

                return await _db.InsertAsync(entry);
            }

            // UPDATE (edit existing) -> DO NOT change EntryDate
            var dbEntry = await _db.Table<JournalEntry>()
                .Where(e => e.EntryId == entry.EntryId)
                .FirstOrDefaultAsync();

            if (dbEntry == null)
                throw new Exception("Journal not found.");

            // Keep original date and created time
            entry.EntryDate = dbEntry.EntryDate;
            entry.CreatedAt = dbEntry.CreatedAt;

            entry.UpdatedAt = DateTime.UtcNow;

            return await _db.UpdateAsync(entry);
        }

        public async Task DeleteEntryAsync(JournalEntry entry)
        {
            await EnsureInitializedAsync();

            var moods = await GetEntryMoodsAsync(entry.EntryId);
            foreach (var m in moods)
                await _db.DeleteAsync(m);

            var tags = await GetEntryTagsAsync(entry.EntryId);
            foreach (var t in tags)
                await _db.DeleteAsync(t);

            await _db.DeleteAsync(entry);
        }

        // ======================= Entry Moods =======================

        public async Task<List<EntryMood>> GetEntryMoodsAsync(int entryId)
        {
            await EnsureInitializedAsync();

            return await _db.Table<EntryMood>()
                .Where(em => em.EntryId == entryId)
                .ToListAsync();
        }

        public async Task SaveEntryMoodsAsync(int entryId, int primaryMoodId, List<int> secondaryMoodIds)
        {
            await EnsureInitializedAsync();

            if (primaryMoodId <= 0)
                throw new Exception("Primary mood is required.");

            var existing = await GetEntryMoodsAsync(entryId);
            foreach (var em in existing)
                await _db.DeleteAsync(em);

            await _db.InsertAsync(new EntryMood
            {
                EntryId = entryId,
                MoodId = primaryMoodId,
                MoodType = "Primary"
            });

            foreach (var secId in secondaryMoodIds
                                 .Where(id => id != primaryMoodId)
                                 .Distinct()
                                 .Take(2))
            {
                await _db.InsertAsync(new EntryMood
                {
                    EntryId = entryId,
                    MoodId = secId,
                    MoodType = "Secondary"
                });
            }
        }

        // ======================= Entry Tags =======================

        public async Task<List<EntryTag>> GetEntryTagsAsync(int entryId)
        {
            await EnsureInitializedAsync();

            return await _db.Table<EntryTag>()
                .Where(et => et.EntryId == entryId)
                .ToListAsync();
        }

        public async Task SaveEntryTagsAsync(int entryId, List<int> tagIds)
        {
            await EnsureInitializedAsync();

            var existing = await GetEntryTagsAsync(entryId);
            foreach (var et in existing)
                await _db.DeleteAsync(et);

            foreach (var tagId in tagIds.Distinct().Take(10))
            {
                await _db.InsertAsync(new EntryTag
                {
                    EntryId = entryId,
                    TagId = tagId
                });
            }
        }

        public async Task<StreakStats> GetStreakStatsAsync(int userId, DateTime? uptoUtcDate = null)
        {
            await EnsureInitializedAsync();

            var upto = (uptoUtcDate ?? DateTime.UtcNow).Date;

            var dates = await _db.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            var entryDates = dates
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var stats = new StreakStats();

            if (entryDates.Count == 0)
                return stats;

            stats.LongestStreak = CalculateLongestStreak(entryDates);
            stats.CurrentStreak = CalculateCurrentStreakEndingAt(entryDates, upto);
            stats.MissedDays = CalculateMissedDays(entryDates, entryDates.First(), upto);

            return stats;
        }

        private static int CalculateLongestStreak(List<DateTime> sortedDates)
        {
            int longest = 1;
            int current = 1;

            for (int i = 1; i < sortedDates.Count; i++)
            {
                var prev = sortedDates[i - 1];
                var curr = sortedDates[i];

                if (curr == prev.AddDays(1))
                {
                    current++;
                    if (current > longest) longest = current;
                }
                else if (curr != prev)
                {
                    current = 1;
                }
            }

            return longest;
        }

        private static int CalculateCurrentStreakEndingAt(List<DateTime> sortedDates, DateTime endDate)
        {
            var set = new HashSet<DateTime>(sortedDates);
            if (!set.Contains(endDate)) return 0;

            int streak = 0;
            var d = endDate;

            while (set.Contains(d))
            {
                streak++;
                d = d.AddDays(-1);
            }

            return streak;
        }

        private static List<DateTime> CalculateMissedDays(List<DateTime> sortedDates, DateTime start, DateTime end)
        {
            var set = new HashSet<DateTime>(sortedDates);
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
