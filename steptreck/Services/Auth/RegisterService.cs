using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.Auth
{
    public class RegisterService
    {
        private readonly AppDbContext _context;
 

        public RegisterService(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task RegisterOrgAsync(RegisterOrgDTO model, CancellationToken ct = default)
        {
            if (model.Password != model.Confirm)
                throw new InvalidOperationException("Пароли не совпадают");

            if (await _context.Organizations.AnyAsync(o => o.Name == model.OrgName, ct))
                throw new InvalidOperationException("Организация уже существует");

            if (await _context.Users.AnyAsync(u => u.CorporateEmail == model.Email, ct))
                throw new InvalidOperationException("Аккаунт с такой почтой уже существует");

            var org = new Organization
            {
                Name = model.OrgName
            };

            var passwordHash = HeasherHelper.HashPassword(model.Password, out var salt);

            var user = new User
            {
                CorporateEmail = model.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                IsActive = true,
                Surname = model.Surname,
                Name = model.Name,
                Patronymic = model.Patronymic
            };
            var member = new Member
            {
                Organization = org,
                User = user,
                RoleId = (int)RoleEnum.Owner,
                IsActive = true,

                Surname = model.Surname,
                Name = model.Name,
                Patronymic = model.Patronymic
            };
            await _context.Organizations.AddAsync(org, ct);
            await _context.Users.AddAsync(user, ct);
            await _context.Members.AddAsync(member, ct);

            await _context.SaveChangesAsync(ct);
        }
        public async Task RegisterUserAsync(RegisterDto model, CancellationToken ct = default)
        {
            if (model.Password != model.Confirm)
                throw new InvalidOperationException("Пароли не совпадают");

            if (await _context.Users.AnyAsync(u => u.CorporateEmail == model.Email, ct))
                throw new InvalidOperationException("Пользователь с таким email уже существует");

            var user = new User
            {
                CorporateEmail = model.Email,
                PasswordHash = HeasherHelper.HashPassword(model.Password, out string salt),
                Salt = salt,
                IsActive = true,
                Surname = model.Surname,
                Name = model.Name,
                Patronymic = model.Patronymic
            };


            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);
        }

    }
}
