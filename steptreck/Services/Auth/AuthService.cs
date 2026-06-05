using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using steptreck.API.Infrastructure.Email;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.API.Services.WorkUser;
using steptreck.Domain.DTOs.AuthDTOs;

namespace steptreck.API.Services.Auth
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly EmailHelper _emailHelper;
        private readonly TokenHelper _tokenHelper;
        private readonly BlockService _blockService;
        private readonly UserHelper _userHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<AuthHub> _authHub;

        public AuthService(AppDbContext context, EmailHelper emailHelper, TokenHelper tokenHelper, BlockService blockService, UserHelper userHelper, IHttpContextAccessor httpContextAccessor, IHubContext<AuthHub> authHub)
        {
            _context = context;
            _emailHelper = emailHelper;
            _tokenHelper = tokenHelper;
            _blockService = blockService;
            _userHelper = userHelper;
            _httpContextAccessor = httpContextAccessor;
            _authHub = authHub;
        }

        public async Task<bool> IsMemberAsync(CancellationToken ct = default)
        {
            var userId = _userHelper.GetCurrentUserId();

            return await _context.ProjectTeamMembers
                .AsNoTracking()
                .AnyAsync(tm =>
                    tm.Member.UserId == userId &&
                    tm.Member.IsActive,
                    ct);
        }

        public async Task SendLoginCodeAsync(LoginDTO model, CancellationToken ct = default)
        {
            var user = await ValidateLoginCredentialsAsync(model, ct);

            var code = GenerateLoginCode(user);
            await _emailHelper.SendConfirmCode(user.CorporateEmail, code);
        }

        public async Task<AuthTokenResponse> VerifyCodeAsync(VerifyCodeDto model, CancellationToken ct = default)
        {
            var code = await _context.ConfirmationCodes
                .Include(c => c.User)
                .FirstOrDefaultAsync(c =>
                    c.User.CorporateEmail == model.Email &&
                    c.Code == model.Code, ct);

            if (code == null)
                throw new InvalidOperationException("Неверный код");

            if (code.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Код подтверждения истек");

            var user = code.User;
            var member = await _context.Members
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive, ct);

            string token;
            int? roleId = null;
            string? roleName = null;

            if (member != null)
            {
                token = _tokenHelper.GenerateOrgToken(user, member);
                roleId = member.RoleId;
                roleName = member.Role?.Name;
            }
            else
            {
                token = _tokenHelper.GenerateAuthToken(user);
            }

            _context.ConfirmationCodes.Remove(code);
            await _context.SaveChangesAsync(ct);

            return new AuthTokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                RoleId = roleId,
                RoleName = roleName
            };
        }
        public async Task<LoginChallenge> CreateLoginChallengeAsync(LoginDTO model, CancellationToken ct)
        {
            var user = await ValidateLoginCredentialsAsync(model, ct);

            var httpContext = _httpContextAccessor.HttpContext;

            var ipAddress = httpContext?.Connection.RemoteIpAddress;

            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var loginChallenge = new LoginChallenge
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Status = "pending",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2)
            };

            _context.LoginChallenges.Add(loginChallenge);
            await _context.SaveChangesAsync(ct);

            return loginChallenge;
        }
        public async Task ConfirmLoginChallengeAsync(Guid challengeId, CancellationToken ct)
        {
            var challenge = await _context.LoginChallenges
                .FirstOrDefaultAsync(x => x.Id == challengeId, ct);

            if (challenge is null)
                throw new InvalidOperationException("Challenge not found.");

            var currentUserId = _userHelper.GetCurrentUserId();
            if (challenge.UserId != currentUserId)
                throw new InvalidOperationException("Challenge not found.");

            if (challenge.Status != "pending")
                throw new InvalidOperationException("Challenge already used.");

            if (challenge.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Challenge expired.");

            challenge.Status = "approved";
            challenge.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            var token = await CreateTokenResponseAsync(challenge.UserId, ct);

            await _authHub.Clients
                .Group($"login-challenge:{challenge.Id}")
                .SendAsync("login-approved", new
                {
                    challengeId = challenge.Id,
                    accessToken = token.AccessToken,
                    tokenType = token.TokenType,
                    roleId = token.RoleId,
                    roleName = token.RoleName
                }, ct);
        }

        public async Task<AuthTokenResponse?> TryCompleteApprovedLoginChallengeAsync(Guid challengeId, CancellationToken ct)
        {
            var challenge = await _context.LoginChallenges
                .FirstOrDefaultAsync(x => x.Id == challengeId, ct);

            if (challenge is null)
                throw new InvalidOperationException("Challenge not found.");

            if (challenge.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Challenge expired.");

            if (challenge.Status == "pending")
                return null;

            if (challenge.Status != "approved")
                throw new InvalidOperationException("Challenge already used.");

            var token = await CreateTokenResponseAsync(challenge.UserId, ct);

            challenge.Status = "completed";
            await _context.SaveChangesAsync(ct);

            return token;
        }


        private async Task<User> ValidateLoginCredentialsAsync(LoginDTO model, CancellationToken ct)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.CorporateEmail == model.Email, ct);

            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            if (!HeasherHelper.VerifyPassword(model.Password, user.PasswordHash, user.Salt))
            {
                user.Attempt++;
                if (user.Attempt > 5)
                {
                    await _blockService.BlockByPassword(user, ct);
                }

                await _context.Users
                    .Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(i => i.SetProperty(u => u.Attempt, user.Attempt), ct);

                throw new InvalidOperationException("Неверный логин или пароль");
            }

            return user;
        }

        private async Task<AuthTokenResponse> CreateTokenResponseAsync(int userId, CancellationToken ct)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            var member = await _context.Members
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive, ct);

            string token;
            int? roleId = null;
            string? roleName = null;

            if (member != null)
            {
                token = _tokenHelper.GenerateOrgToken(user, member);
                roleId = member.RoleId;
                roleName = member.Role?.Name;
            }
            else
            {
                token = _tokenHelper.GenerateAuthToken(user);
            }

            return new AuthTokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                RoleId = roleId,
                RoleName = roleName
            };
        }



        private string GenerateLoginCode(User user)
        {
            var code = new Random().Next(100000, 1000000).ToString();

            var verCode = new ConfirmationCode
            {
                UserId = user.Id,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(10)
            };

            _context.ConfirmationCodes.Add(verCode);
            _context.SaveChanges();
            return code;
        }

    }
}
 
