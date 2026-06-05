namespace steptreck.Domain.DTOs
{
    public class CreateTeamNotificationDto
    {
        public int TeamId { get; set; }
        public string Text { get; set; } = "";
    }

    public class CreateMemberNotificationDto
    {
        public int TeamId { get; set; }
        public int MemberId { get; set; }
        public string Text { get; set; } = "";
    }
}
