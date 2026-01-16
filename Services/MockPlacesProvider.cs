namespace TravelPlanner.Api.Services;

public class MockPlacesProvider : IPlacesProvider
{
    private readonly List<PlaceSearchResult> _mockPlaces = new()
    {
        new PlaceSearchResult
        {
            Id = "1",
            Name = "Голубая мечеть",
            Category = "Музеи",
            Address = "Sultanahmet, Fatih",
            Latitude = 41.0053,
            Longitude = 28.9769,
            Rating = 4.8,
            Description = "Одна из самых известных и величественных мечетей Стамбула",
            Hours = "Открыто • Закрывается в 18:30",
            Phone = "+90 212 458 44 68"
        },
        new PlaceSearchResult
        {
            Id = "2",
            Name = "Собор Святой Софии",
            Category = "Музеи",
            Address = "Sultanahmet Mah., Fatih",
            Latitude = 41.0086,
            Longitude = 28.9802,
            Rating = 4.9
        },
        new PlaceSearchResult
        {
            Id = "3",
            Name = "Balkan Lokantası",
            Category = "Еда",
            Address = "Istiklal Caddesi",
            Latitude = 41.0369,
            Longitude = 28.9850,
            Rating = 4.6
        },
        new PlaceSearchResult
        {
            Id = "4",
            Name = "Кафе Mandabatmaz",
            Category = "Кофе",
            Address = "Beyoğlu",
            Latitude = 41.0331,
            Longitude = 28.9789,
            Rating = 4.7
        },
        new PlaceSearchResult
        {
            Id = "5",
            Name = "Гранд-базар",
            Category = "Прогулка",
            Address = "Beyazıt, Fatih",
            Latitude = 41.0106,
            Longitude = 28.9681,
            Rating = 4.5
        }
    };

    public async Task<List<PlaceSearchResult>> SearchAsync(string query, string? category = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken); // Симуляция задержки

        var results = _mockPlaces
            .Where(p => 
                p.Name.ToLower().Contains(query.ToLower()) &&
                (category == null || p.Category == category)
            )
            .ToList();

        return results;
    }

    public async Task<PlaceSearchResult?> GetDetailsAsync(string placeId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken);
        return _mockPlaces.FirstOrDefault(p => p.Id == placeId);
    }
}
