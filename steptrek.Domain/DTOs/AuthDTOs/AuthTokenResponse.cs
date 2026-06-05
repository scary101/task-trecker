namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class AuthTokenResponse
    {
        public string AccessToken { get; init; } = null!;
        public string TokenType { get; init; } = "Bearer";
        public int? RoleId { get; init; }
        public string? RoleName { get; init; }
    }
}
