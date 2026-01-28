using Journal.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Colors = QuestPDF.Helpers.Colors;

namespace Journal.Services
{
    public class PdfExportService : DatabaseService
    {
        private bool _initialized;

        // ---------------------------------------------------------
        // Public API
        // ---------------------------------------------------------

        /// <summary>Export all entries for a specific user.</summary>
        public async Task<string> ExportUserEntriesAsync(int userId)
        {
            await EnsureInitializedAsync();

            var user = await _db.Table<User>()
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            var entries = await _db.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return await GenerateAndSavePdfAsync(entries, user?.Username ?? $"User {userId}");
        }

        /// <summary>Export all entries (single-user apps often use this).</summary>
        public async Task<string> ExportAllEntriesAsync()
        {
            await EnsureInitializedAsync();

            var entries = await _db.Table<JournalEntry>()
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return await GenerateAndSavePdfAsync(entries, "All Entries");
        }

        // Export for one user within a date range (inclusive)
        public async Task<string> ExportUserEntriesByDateRangeAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            await EnsureInitializedAsync();

            var from = fromDate.Date;
            var to = toDate.Date;

            var user = await _db.Table<User>()
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            var entries = await _db.Table<JournalEntry>()
                .Where(e => e.UserId == userId && e.EntryDate >= from && e.EntryDate <= to)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            var title = $"{(user?.Username ?? $"User {userId}")} ({from:yyyy-MM-dd} to {to:yyyy-MM-dd})";
            return await GenerateAndSavePdfAsync(entries, title);
        }

        // Export all entries within a date range
        public async Task<string> ExportAllEntriesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            await EnsureInitializedAsync();

            var from = fromDate.Date;
            var to = toDate.Date;

            var entries = await _db.Table<JournalEntry>()
                .Where(e => e.EntryDate >= from && e.EntryDate <= to)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            var title = $"All Entries ({from:yyyy-MM-dd} to {to:yyyy-MM-dd})";
            return await GenerateAndSavePdfAsync(entries, title);
        }

        // ---------------------------------------------------------
        // Ensure tables exist (safe)
        // ---------------------------------------------------------
        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await _db.CreateTableAsync<User>();
            await _db.CreateTableAsync<JournalEntry>();
            await _db.CreateTableAsync<EntryMood>();
            await _db.CreateTableAsync<EntryTag>();
            await _db.CreateTableAsync<Mood>();
            await _db.CreateTableAsync<Tag>();

            _initialized = true;
        }

        // ---------------------------------------------------------
        // Core PDF generator
        // ---------------------------------------------------------
        private async Task<string> GenerateAndSavePdfAsync(List<JournalEntry> entries, string headerTitle)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Lookup tables
            var moods = await _db.Table<Mood>().ToListAsync();
            var tags = await _db.Table<Tag>().ToListAsync();

            var moodById = moods.ToDictionary(m => m.MoodId, m => m);
            var tagById = tags.ToDictionary(t => t.TagId, t => t);

            // Relations
            var entryIds = entries.Select(e => e.EntryId).ToList();

            var entryMoods = entryIds.Count == 0
                ? new List<EntryMood>()
                : await _db.Table<EntryMood>()
                    .Where(em => entryIds.Contains(em.EntryId))
                    .ToListAsync();

            var entryTags = entryIds.Count == 0
                ? new List<EntryTag>()
                : await _db.Table<EntryTag>()
                    .Where(et => entryIds.Contains(et.EntryId))
                    .ToListAsync();

            var moodsByEntry = entryMoods
                .GroupBy(x => x.EntryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tagsByEntry = entryTags
                .GroupBy(x => x.EntryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    // Header
                    page.Header().Column(h =>
                    {
                        h.Spacing(4);

                        h.Item().Text("LogBook Journal Export")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#2563EB")
                            .AlignCenter();

                        h.Item().Text(headerTitle)
                            .FontSize(12)
                            .FontColor("#374151")
                            .AlignCenter();

                        h.Item().LineHorizontal(1).LineColor("#E5E7EB");
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(10);

                        if (entries.Count == 0)
                        {
                            col.Item().Text("No journal entries found.")
                                .FontSize(12)
                                .FontColor("#374151");
                            return;
                        }

                        foreach (var entry in entries)
                        {
                            moodsByEntry.TryGetValue(entry.EntryId, out var relMoods);
                            tagsByEntry.TryGetValue(entry.EntryId, out var relTags);

                            // Primary mood id (Category mood row: 1..3)
                            var primaryMoodId = relMoods?
                                .FirstOrDefault(x => x.MoodType == "Primary")
                                ?.MoodId;

                            string primaryCategory = "—";
                            if (primaryMoodId.HasValue && moodById.TryGetValue(primaryMoodId.Value, out var primaryMood))
                            {
                                primaryCategory = primaryMood.MoodCategory == "Category"
                                    ? primaryMood.MoodName
                                    : primaryMood.MoodCategory;
                            }

                            var secondaryNames = relMoods?
                                .Where(x => x.MoodType == "Secondary")
                                .Select(x => moodById.TryGetValue(x.MoodId, out var m) ? m.MoodName : $"Mood {x.MoodId}")
                                .Distinct()
                                .Take(2)
                                .ToList() ?? new List<string>();

                            var secondaryText = secondaryNames.Count > 0 ? string.Join(", ", secondaryNames) : "—";

                            var tagNames = relTags?
                                .Select(x => tagById.TryGetValue(x.TagId, out var t) ? t.Tagname : $"Tag {x.TagId}")
                                .Distinct()
                                .Take(10)
                                .ToList() ?? new List<string>();

                            var tagsText = tagNames.Count > 0 ? string.Join(", ", tagNames) : "—";

                            col.Item().Border(1)
                                .BorderColor("#E5E7EB")
                                .Background(Colors.White)
                                .Padding(12)
                                .Column(card =>
                                {
                                    card.Spacing(6);

                                    card.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(entry.Title ?? "")
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor(Colors.Black);

                                        row.ConstantItem(160).AlignRight()
                                            .Text(entry.EntryDate.ToString("yyyy-MM-dd"))
                                            .FontSize(11)
                                            .FontColor("#6B7280");
                                    });

                                    card.Item().Text($"Primary: {primaryCategory}")
                                        .FontColor("#2563EB")
                                        .SemiBold();

                                    card.Item().Text($"Secondary: {secondaryText}")
                                        .FontColor("#374151");

                                    card.Item().Text($"Tags: {tagsText}")
                                        .FontColor("#374151");

                                    card.Item().LineHorizontal(1).LineColor("#E5E7EB");

                                    card.Item().Text(string.IsNullOrWhiteSpace(entry.Content) ? "—" : entry.Content)
                                        .FontColor(Colors.Black);

                                    card.Item().PaddingTop(6)
                                        .Text($"Words: {entry.WordCount}")
                                        .FontSize(10)
                                        .FontColor("#6B7280");
                                });
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generated on ").FontColor("#6B7280");
                        txt.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).SemiBold().FontColor("#111827");
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();

            var fileName = $"LogBook_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            var filePath = GetDownloadPath(fileName);

            File.WriteAllBytes(filePath, pdfBytes);

            return filePath;
        }

        // ---------------------------------------------------------
        // Save destination
        // ---------------------------------------------------------
        private string GetDownloadPath(string fileName)
        {
#if ANDROID
            return Path.Combine(
                Android.OS.Environment
                    .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)
                    .AbsolutePath,
                fileName
            );
#elif WINDOWS
            var downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );
            return Path.Combine(downloads, fileName);
#else
            return Path.Combine(FileSystem.AppDataDirectory, fileName);
#endif
        }
    }
}
