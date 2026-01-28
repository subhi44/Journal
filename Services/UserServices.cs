using Journal.Models;

namespace Journal.Services
{
    public class UserService : DatabaseService
    {
        private bool _initialized;

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.CreateTableAsync<User>();
            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        public async Task<List<User>> GetUsersAsync()
        {
            await EnsureInitializedAsync();
            return await _db.Table<User>().ToListAsync();
        }

        public async Task<int> SaveUserAsync(User user)
        {
            await EnsureInitializedAsync();

            // Same logic: update if existing (UserId != 0), else insert
            return user.UserId != 0
                ? await _db.UpdateAsync(user)
                : await _db.InsertAsync(user);
        }

        // Optional: seed admin user (call once from startup if you want)
        public async Task SeedAdminAsync(string adminPassword)
        {
            await EnsureInitializedAsync();

            const string adminUsername = "admin";

            var existing = await _db.Table<User>()
                .Where(u => u.Username == adminUsername)
                .FirstOrDefaultAsync();

            if (existing != null) return;

            var admin = new User
            {
                Username = adminUsername,
                PasswordHash = SecurityService.HashPassword(adminPassword),
                CreatedAt = DateTime.UtcNow,
                LastLogin = null
            };

            await _db.InsertAsync(admin);
        }

        public async Task<int> DeleteUserAsync(User user)
        {
            await EnsureInitializedAsync();
            return await _db.DeleteAsync(user);
        }
    }
}
