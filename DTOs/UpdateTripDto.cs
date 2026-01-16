namespace TravelPlanner.Api.DTOs;

public class UpdateTripDto
{
    public string? Title { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
