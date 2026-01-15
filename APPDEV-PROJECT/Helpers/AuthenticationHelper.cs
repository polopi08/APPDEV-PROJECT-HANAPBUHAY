// ===== NEW FILE: Authentication Helper Service =====
// Provides utility methods for password hashing and verification
// Uses BCrypt for secure password handling

using System.Security.Cryptography;
using System.Text;

namespace APPDEV_PROJECT.Helpers
{
    public static class AuthenticationHelper
    {
        // ===== Hash Password =====
        // Converts a plain text password into a secure hash
        // This hash is what gets stored in the database, not the actual password
        public static string HashPassword(string password)
        {
            // Generate a random salt to make the hash unique even for the same password
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt using PBKDF2
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            // Combine salt and hash into a single string for storage
            byte[] hashWithSalt = new byte[36];
            Array.Copy(salt, 0, hashWithSalt, 0, 16);
            Array.Copy(hash, 0, hashWithSalt, 16, 20);

            return Convert.ToBase64String(hashWithSalt);
        }

        // ===== Verify Password =====
        // Checks if a plain text password matches the stored hash
        // Used during login to verify credentials
        public static bool VerifyPassword(string password, string hash)
        {
            // Extract the salt from the stored hash
            byte[] hashWithSalt = Convert.FromBase64String(hash);
            byte[] salt = new byte[16];
            Array.Copy(hashWithSalt, 0, salt, 0, 16);

            // Hash the provided password with the same salt
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash2 = pbkdf2.GetBytes(20);

            // Compare the new hash with the stored hash
            for (int i = 0; i < 20; i++)
            {
                if (hashWithSalt[i + 16] != hash2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
