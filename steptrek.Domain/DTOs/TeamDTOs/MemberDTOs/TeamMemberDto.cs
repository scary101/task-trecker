namespace steptreck.Domain.DTOs.TeamDTOs.MemberDTOs
{
    public class TeamMemberDto
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string? TeamRole { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
