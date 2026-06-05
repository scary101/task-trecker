namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class LoginChallengeCreatedDto
    {
        public Guid ChallengeId { get; set; }
        public int ExpiresInSeconds { get; set; }
    }
}
