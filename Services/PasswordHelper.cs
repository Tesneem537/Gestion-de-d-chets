using BCrypt.Net;

namespace WasteManagement3.Services
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string entered, string hashed)
        {
            return BCrypt.Net.BCrypt.Verify(entered, hashed);
        }
    }
}
