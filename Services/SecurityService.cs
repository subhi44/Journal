using Journal.Models;
using System.Security.Cryptography;
using System.Text;

namespace Journal.Services
{
    public class SecurityService : DatabaseService
    {
        private bool _initialized;

        // Session flag
        public bool IsUnlocked { get; private set; }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _db.CreateTableAsync<User>();
            _initialized = true;
        }

        public async Task InitializeAsync() => await EnsureInitializedAsync();

        public async Task<bool> IsUserSetAsync()
        {
            await EnsureInitializedAsync();
            return await _db.Table<User>().CountAsync() > 0;
        }

        // Create first user (single-user app)
        public async Task SetUserAsync(string username, string pin)
        {
            await EnsureInitializedAsync();

            username = (username ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Username is required.");

            if (string.IsNullOrWhiteSpace(pin))
                throw new Exception("PIN cannot be empty.");

            if (pin.Length < 6 || pin.Length > 15)
                throw new Exception("PIN must be between 6 and 15 characters.");

            var existing = await _db.Table<User>().FirstOrDefaultAsync();
            if (existing != null)
                throw new Exception("User already exists. Reset is required to set a new PIN.");

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(pin),
                CreatedAt = DateTime.UtcNow,
                LastLogin = null
            };

            await _db.InsertAsync(user);

            IsUnlocked = false;
        }

        public async Task<bool> VerifyPinAsync(string pin)
        {
            await EnsureInitializedAsync();

            if (string.IsNullOrWhiteSpace(pin)) return false;

            var user = await _db.Table<User>().FirstOrDefaultAsync();
            if (user == null) return false;

            if (user.PasswordHash == HashPassword(pin))
            {
                IsUnlocked = true;
                user.LastLogin = DateTime.UtcNow;

                // same behavior as before
                await _db.UpdateAsync(user);

                return true;
            }

            return false;
        }

        public void Lock() => IsUnlocked = false;

        public async Task<bool> IsPinRequiredAsync()
        {
            await EnsureInitializedAsync();

            var user = await _db.Table<User>().FirstOrDefaultAsync();
            if (user == null) return false;

            // same logic as before
            return !IsUnlocked;
        }

        public static string HashPassword(string value)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToBase64String(bytes);
        }
    }
}
