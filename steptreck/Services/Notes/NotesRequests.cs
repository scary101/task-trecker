namespace steptreck.API.Services.Notes
{
    public class CreateNoteRequest
    {
        public string Title { get; set; } = "";
    }

    public class UpdateContentRequest
    {
        public string Content { get; set; } = "";
    }

    public class UpdateTitleRequest
    {
        public string Title { get; set; } = "";
    }
}
