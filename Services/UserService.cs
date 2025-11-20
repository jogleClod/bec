using MobileAppBackend.Data;
using MobileAppBackend.Models;
using MobileAppBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MobileAppBackend.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(UserDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                throw new Exception("Пользователь с таким email уже существует");
            }

            var passwordHash = HashPassword(userDto.Password);

            var user = new User
            {
                Email = userDto.Email,
                PasswordHash = passwordHash,
                Name = userDto.Name
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new Exception("Неверный email или пароль");
            }

            return user;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }
    }
}