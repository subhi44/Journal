using Journal.Models;

namespace Journal.Services
{
    public class TagService : DatabaseService
    {
        private bool _initialized;

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.CreateTableAsync<Tag>();
            await SeedPredefinedTagsAsync(); // seed once

            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        public async Task<List<Tag>> GetTagsAsync()
        {
            await EnsureInitializedAsync();

            return await _db.Table<Tag>()
                .OrderByDescending(t => t.IsPredefined) // predefined first
                .ThenBy(t => t.Tagname)
                .ToListAsync();
        }

        public async Task<int> SaveTagAsync(Tag tag)
        {
            await EnsureInitializedAsync();

            // Insert (user-created tag)
            if (tag.TagId == 0)
            {
                tag.IsPredefined = false;
                return await _db.InsertAsync(tag);
            }

            // Update (prevent editing predefined tags)
            var existing = await _db.Table<Tag>()
                .Where(t => t.TagId == tag.TagId)
                .FirstOrDefaultAsync();

            if (existing == null) return 0;

            if (existing.IsPredefined)
                throw new Exception("Predefined tags cannot be edited.");

            tag.IsPredefined = false;
            return await _db.UpdateAsync(tag);
        }

        public async Task<int> DeleteTagAsync(Tag tag)
        {
            await EnsureInitializedAsync();

            // Prevent deleting predefined tags
            var existing = await _db.Table<Tag>()
                .Where(t => t.TagId == tag.TagId)
                .FirstOrDefaultAsync();

            if (existing == null) return 0;

            if (existing.IsPredefined)
                throw new Exception("Predefined tags cannot be deleted.");

            return await _db.DeleteAsync(existing);
        }

        // =====================================================
        // PREDEFINED TAGS (SEEDING)
        // =====================================================
        private async Task SeedPredefinedTagsAsync()
        {
            // If predefined tags already exist correctly, skip
            var predefinedCount = await _db.Table<Tag>()
                .Where(t => t.IsPredefined)
                .CountAsync();

            if (predefinedCount == 31) return; // total predefined tags

            // 1) Clear table
            await _db.ExecuteAsync("DELETE FROM Tag;");

            // 2) Reset AUTOINCREMENT
            await _db.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name='Tag';");

            // 3) Insert in fixed order (IDs become 1..31)
            var predefined = new List<Tag>
            {
                new Tag { Tagname = "Work", IsPredefined = true },
                new Tag { Tagname = "Career", IsPredefined = true },
                new Tag { Tagname = "Studies", IsPredefined = true },
                new Tag { Tagname = "Family", IsPredefined = true },
                new Tag { Tagname = "Friends", IsPredefined = true },
                new Tag { Tagname = "Relationships", IsPredefined = true },
                new Tag { Tagname = "Health", IsPredefined = true },
                new Tag { Tagname = "Fitness", IsPredefined = true },
                new Tag { Tagname = "Personal Growth", IsPredefined = true },
                new Tag { Tagname = "Self-care", IsPredefined = true },
                new Tag { Tagname = "Hobbies", IsPredefined = true },
                new Tag { Tagname = "Travel", IsPredefined = true },
                new Tag { Tagname = "Nature", IsPredefined = true },
                new Tag { Tagname = "Finance", IsPredefined = true },
                new Tag { Tagname = "Spirituality", IsPredefined = true },
                new Tag { Tagname = "Birthday", IsPredefined = true },
                new Tag { Tagname = "Holiday", IsPredefined = true },
                new Tag { Tagname = "Vacation", IsPredefined = true },
                new Tag { Tagname = "Celebration", IsPredefined = true },
                new Tag { Tagname = "Exercise", IsPredefined = true },
                new Tag { Tagname = "Reading", IsPredefined = true },
                new Tag { Tagname = "Writing", IsPredefined = true },
                new Tag { Tagname = "Cooking", IsPredefined = true },
                new Tag { Tagname = "Meditation", IsPredefined = true },
                new Tag { Tagname = "Yoga", IsPredefined = true },
                new Tag { Tagname = "Music", IsPredefined = true },
                new Tag { Tagname = "Shopping", IsPredefined = true },
                new Tag { Tagname = "Parenting", IsPredefined = true },
                new Tag { Tagname = "Projects", IsPredefined = true },
                new Tag { Tagname = "Planning", IsPredefined = true },
                new Tag { Tagname = "Reflection", IsPredefined = true }
            };

            await _db.InsertAllAsync(predefined);
        }
    }
}
