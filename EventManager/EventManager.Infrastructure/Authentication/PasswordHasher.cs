using EventManager.Application.Interfaces.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace EventManager.Infrastructure.Authentication
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            return Convert.ToHexString(hashBytes);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
                return false;

            string hash = HashPassword(password);

            return string.Equals(hash,
                                 passwordHash,
                                 StringComparison.OrdinalIgnoreCase);
        }
    }
}
