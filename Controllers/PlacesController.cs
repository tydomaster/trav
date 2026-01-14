using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/places")]
public class PlacesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/trips/{tripId}/places - Получить все места из itinerary
    [HttpGet]
    public async Task<ActionResult<List<PlaceDto>>> GetPlaces(int tripId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var hasAccess = await _context.Memberships
            .AnyAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (!hasAccess)
            return StatusCode(403, new { error = "Access denied", message = "You do not have access to this trip" });

        // Получаем все места из items поездки
        // Используем Join для правильной работы с nullable PlaceId
        var places = await _context.Items
            .Where(i => i.Day.TripId == tripId && i.PlaceId != null)
            .Join(
                _context.Places,
                item => item.PlaceId,
                place => place.Id,
                (item, place) => place
            )
            .Distinct()
            .Select(p => new PlaceDto
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Address = p.Address,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(places);
    }

    // GET: api/trips/{tripId}/places/{placeId} - Получить конкретное место
    [HttpGet("{placeId}")]
    public async Task<ActionResult<PlaceDto>> GetPlace(int tripId, int placeId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var hasAccess = await _context.Memberships
            .AnyAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (!hasAccess)
            return StatusCode(403, new { error = "Access denied", message = "You do not have access to this trip" });

        var place = await _context.Places
            .FirstOrDefaultAsync(p => p.Id == placeId);

        if (place == null)
            return NotFound();

        // Проверяем, что место используется в этой поездке
        var isUsedInTrip = await _context.Items
            .AnyAsync(i => i.Day.TripId == tripId && i.PlaceId == placeId);

        if (!isUsedInTrip)
            return StatusCode(403, new { error = "Access denied", message = "Place is not used in this trip" });

        var placeDto = new PlaceDto
        {
            Id = place.Id,
            Name = place.Name,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            Address = place.Address,
            Description = place.Description,
            CreatedAt = place.CreatedAt,
            UpdatedAt = place.UpdatedAt
        };

        return Ok(placeDto);
    }

    // POST: api/trips/{tripId}/places - Создать место
    [HttpPost]
    public async Task<ActionResult<PlaceDto>> CreatePlace(int tripId, CreatePlaceDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут создавать места)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can create places" });

        var place = new Place
        {
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Places.Add(place);
        await _context.SaveChangesAsync();

        var placeDto = new PlaceDto
        {
            Id = place.Id,
            Name = place.Name,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            Address = place.Address,
            Description = place.Description,
            CreatedAt = place.CreatedAt,
            UpdatedAt = place.UpdatedAt
        };

        return CreatedAtAction(nameof(GetPlace), new { tripId, placeId = place.Id }, placeDto);
    }

    // PUT: api/trips/{tripId}/places/{placeId} - Обновить место
    [HttpPut("{placeId}")]
    public async Task<ActionResult<PlaceDto>> UpdatePlace(int tripId, int placeId, UpdatePlaceDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут обновлять места)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can update places" });

        var place = await _context.Places
            .FirstOrDefaultAsync(p => p.Id == placeId);

        if (place == null)
            return NotFound();

        // Проверяем, что место используется в этой поездке
        var isUsedInTrip = await _context.Items
            .AnyAsync(i => i.Day.TripId == tripId && i.PlaceId == placeId);

        if (!isUsedInTrip)
            return StatusCode(403, new { error = "Access denied", message = "Place is not used in this trip" });

        place.Name = dto.Name;
        place.Latitude = dto.Latitude;
        place.Longitude = dto.Longitude;
        place.Address = dto.Address;
        place.Description = dto.Description;
        place.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var placeDto = new PlaceDto
        {
            Id = place.Id,
            Name = place.Name,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            Address = place.Address,
            Description = place.Description,
            CreatedAt = place.CreatedAt,
            UpdatedAt = place.UpdatedAt
        };

        return Ok(placeDto);
    }
}

