using Journal.Models;

namespace Journal.Services
{
    public class MoodService : DatabaseService
    {
        private bool _initialized;

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.CreateTableAsync<Mood>();
            await SeedPredefinedMoodsAsync();

            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        public async Task<List<Mood>> GetMoodsAsync()
        {
            await EnsureInitializedAsync();

            return await _db.Table<Mood>()
                .OrderBy(m => m.MoodId)
                .ToListAsync();
        }

        // =====================================================
        // PREDEFINED MOODS WITH FIXED IDS (1–18)
        // 1–3 = CATEGORY moods (Primary stored here)
        // 4–18 = Real moods (Secondary options)
        // =====================================================
        private async Task SeedPredefinedMoodsAsync()
        {
            var existingCount = await _db.Table<Mood>().CountAsync();
            if (existingCount == 18) return;

            // 1) Clear table
            await _db.ExecuteAsync("DELETE FROM Mood;");

            // 2) Reset AUTOINCREMENT counter
            await _db.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name='Mood';");

            // 3) Insert moods in fixed order => IDs become fixed (1..18)
            var predefinedMoods = new List<Mood>
            {
                // CATEGORY moods (Primary)
                new Mood { MoodName = "Positive", MoodCategory = "Category" }, // MoodId 1
                new Mood { MoodName = "Neutral",  MoodCategory = "Category" }, // MoodId 2
                new Mood { MoodName = "Negative", MoodCategory = "Category" }, // MoodId 3

                // Positive (Secondary choices)
                new Mood { MoodName = "Happy",     MoodCategory = "Positive" },
                new Mood { MoodName = "Excited",   MoodCategory = "Positive" },
                new Mood { MoodName = "Relaxed",   MoodCategory = "Positive" },
                new Mood { MoodName = "Grateful",  MoodCategory = "Positive" },
                new Mood { MoodName = "Confident", MoodCategory = "Positive" },

                // Neutral (Secondary choices)
                new Mood { MoodName = "Calm",       MoodCategory = "Neutral" },
                new Mood { MoodName = "Thoughtful", MoodCategory = "Neutral" },
                new Mood { MoodName = "Curious",    MoodCategory = "Neutral" },
                new Mood { MoodName = "Nostalgic",  MoodCategory = "Neutral" },
                new Mood { MoodName = "Bored",      MoodCategory = "Neutral" },

                // Negative (Secondary choices)
                new Mood { MoodName = "Sad",      MoodCategory = "Negative" },
                new Mood { MoodName = "Angry",    MoodCategory = "Negative" },
                new Mood { MoodName = "Stressed", MoodCategory = "Negative" },
                new Mood { MoodName = "Lonely",   MoodCategory = "Negative" },
                new Mood { MoodName = "Anxious",  MoodCategory = "Negative" }
            };

            await _db.InsertAllAsync(predefinedMoods);
        }
    }
}
