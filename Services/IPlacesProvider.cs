namespace TravelPlanner.Api.Services;

public class PlaceSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Rating { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Hours { get; set; }
}

public interface IPlacesProvider
{
    Task<List<PlaceSearchResult>> SearchAsync(string query, string? category = null, CancellationToken cancellationToken = default);
    Task<PlaceSearchResult?> GetDetailsAsync(string placeId, CancellationToken cancellationToken = default);
}
