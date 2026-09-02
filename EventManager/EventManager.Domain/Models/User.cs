using EventManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Domain.Models
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Login { get; private set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }

        // Конструктор для ORM / десериализации
        private User() { }

        private User(Guid id,
                     string login,
                     string passwordHash,
                     UserRole role)
        {
            Id = id;
            Login = login;
            PasswordHash = passwordHash;
            Role = role;
        }

        public static User Create(string login, string passwordHash, UserRole role = UserRole.User)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Хеш пароля не может быть пустым.", nameof(passwordHash));

            return new User(Guid.NewGuid(), login.Trim(), passwordHash, role);
        }

        public bool IsAdmin => Role == UserRole.Admin;
    }
}
