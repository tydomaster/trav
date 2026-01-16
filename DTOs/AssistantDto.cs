namespace TravelPlanner.Api.DTOs;

public class AssistantMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public List<AssistantSuggestionDto>? Suggestions { get; set; }
}

public class AssistantSuggestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SendMessageDto
{
    public string Message { get; set; } = string.Empty;
}

public class ApplySuggestionDto
{
    public string SuggestionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "itinerary" or "list"
}
