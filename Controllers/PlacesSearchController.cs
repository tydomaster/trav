using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/places")]
public class PlacesSearchController : ControllerBase
{
    private readonly IPlacesProvider _placesProvider;
    private readonly ApplicationDbContext _context;

    public PlacesSearchController(IPlacesProvider placesProvider, ApplicationDbContext context)
    {
        _placesProvider = placesProvider;
        _context = context;
    }

    // GET: api/places/search?query=мечеть&category=Музеи
    [HttpGet("search")]
    public async Task<ActionResult> SearchPlaces([FromQuery] string query, [FromQuery] string? category)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var results = await _placesProvider.SearchAsync(query, category);

        return Ok(results);
    }

    // GET: api/places/{placeId}/details
    [HttpGet("{placeId}/details")]
    public async Task<ActionResult> GetPlaceDetails(string placeId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var details = await _placesProvider.GetDetailsAsync(placeId);

        if (details == null)
            return NotFound();

        return Ok(details);
    }

    // POST: api/trips/{tripId}/places/search/add
    [HttpPost("trips/{tripId}/add")]
    public async Task<ActionResult> AddPlaceFromSearch(int tripId, [FromBody] AddPlaceFromSearchDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут добавлять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can add places" });

        // Получаем детали места из провайдера
        var placeDetails = await _placesProvider.GetDetailsAsync(dto.PlaceId);
        if (placeDetails == null)
            return NotFound(new { error = "Place not found" });

        // Создаем место в базе данных
        var place = new TravelPlanner.Api.Models.Place
        {
            TripId = tripId,
            AddedByUserId = userIdValue,
            Name = placeDetails.Name,
            Latitude = placeDetails.Latitude ?? 0,
            Longitude = placeDetails.Longitude ?? 0,
            Address = placeDetails.Address,
            Description = placeDetails.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Places.Add(place);
        await _context.SaveChangesAsync();

        return Ok(new { id = place.Id, message = "Place added successfully" });
    }
}

public class AddPlaceFromSearchDto
{
    public string PlaceId { get; set; } = string.Empty;
}
