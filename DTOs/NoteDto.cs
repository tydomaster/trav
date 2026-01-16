namespace TravelPlanner.Api.DTOs;

public class NoteDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateNoteDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class UpdateNoteDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}
