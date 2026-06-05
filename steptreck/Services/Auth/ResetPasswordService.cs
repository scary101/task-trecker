using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Email;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.AuthDTOs;

namespace steptreck.API.Services.Auth
{
    public class ResetPasswordService
    {
        private readonly AppDbContext _context;
        private readonly TokenHelper _tokenHelper;
        private readonly EmailHelper _emailHelper;

        public ResetPasswordService(AppDbContext context, TokenHelper tokenHelper, EmailHelper emailHelper)
        {
            _context = context;
            _tokenHelper = tokenHelper;
            _emailHelper = emailHelper;
        }

        public async Task SendResetLinkAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CorporateEmail == email);
            if (user == null) return;

            var userBans = await _context.UserLocks.Where(i => i.User == user).ToListAsync();
            if (userBans.Any(b => b.UnlockAt > DateTime.UtcNow))
                throw new InvalidOperationException("Пользователь заблокирован");

            var token = _tokenHelper.GeneratePasswordResetToken();

            _context.PasswordResets.Add(new PasswordReset
            {
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                UserId = user.Id,
                Used = false
            });

            await _context.SaveChangesAsync();
            await _emailHelper.SendPasswordReset(email, token);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto model)
        {
            model.Token = model.Token.Replace(" ", "+");
            var tokenRecord = await _context.PasswordResets
                .FirstOrDefaultAsync(t => t.Token == model.Token);

            if (tokenRecord == null || tokenRecord.Used || tokenRecord.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Ссылка недействительна или истекла");

            if (model.Password != model.Reset)
                throw new InvalidOperationException("Пароли не совпадают");

            var user = await _context.Users.FindAsync(tokenRecord.UserId);
            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            user.PasswordHash = HeasherHelper.HashPassword(model.Password, out string salt);
            user.Salt = salt;

            tokenRecord.Used = true;

            await _context.SaveChangesAsync();
        }
        public async Task CheckTokenReset(string token)
        {
            token = token.Replace(" ", "+");
            var tokenRecord = await _context.PasswordResets
                .FirstOrDefaultAsync(t => t.Token == token && !t.Used);

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Ссылка недействительна или истекла.");
        }
    }
}
