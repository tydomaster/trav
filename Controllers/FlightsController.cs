using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/flights")]
public class FlightsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(ApplicationDbContext context, ILogger<FlightsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/trips/{tripId}/flights?category=1
    [HttpGet]
    public async Task<ActionResult<List<FlightDto>>> GetFlights(int tripId, [FromQuery] int? category)
    {
        try
        {
            _logger.LogInformation("GetFlights called for tripId: {TripId}, category: {Category}", tripId, category);

            var userId = User.GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetFlights: User not authenticated");
                return Unauthorized();
            }

            var userIdValue = userId.Value;
            _logger.LogInformation("GetFlights: UserId: {UserId}", userIdValue);

            // Проверяем доступ к поездке
            var hasAccess = await _context.Memberships
                .AnyAsync(m => m.TripId == tripId && m.UserId == userIdValue);

            if (!hasAccess)
            {
                _logger.LogWarning("GetFlights: Access denied for UserId: {UserId}, TripId: {TripId}", userIdValue, tripId);
                return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });
            }

            _logger.LogInformation("GetFlights: Building query for TripId: {TripId}", tripId);
            var query = _context.Flights
                .AsNoTracking() // Не отслеживаем изменения, только читаем
                .Where(f => f.TripId == tripId);

            if (category.HasValue)
            {
                query = query.Where(f => f.Category == (BookingCategory)category.Value);
                _logger.LogInformation("GetFlights: Filtering by category: {Category}", category.Value);
            }

            _logger.LogInformation("GetFlights: Executing query");
            List<Flight> flights;
            try
            {
                flights = await query
                    .OrderBy(f => f.Date)
                    .ThenBy(f => f.Time.HasValue ? f.Time.Value : TimeSpan.MaxValue) // Обрабатываем null для сортировки
                    .ToListAsync();
                _logger.LogInformation("GetFlights: Query executed successfully, found {Count} flights", flights.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing flights query for TripId: {TripId}", tripId);
                throw;
            }

            var flightDtos = new List<FlightDto>();
            foreach (var f in flights)
            {
                try
                {
                    var dto = new FlightDto
                    {
                        Id = f.Id,
                        TripId = f.TripId,
                        Category = (int)f.Category,
                        Type = (int)f.Type,
                        Title = f.Title ?? string.Empty,
                        Subtitle = f.Subtitle ?? string.Empty,
                        From = f.From ?? string.Empty,
                        To = f.To ?? string.Empty,
                        Date = f.Date,
                        Time = f.Time.HasValue ? $"{f.Time.Value.Hours:D2}:{f.Time.Value.Minutes:D2}" : null,
                        Status = f.Status.HasValue ? (int)f.Status.Value : null,
                        Details = f.Details,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    };
                    flightDtos.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error mapping flight {FlightId} to DTO. Flight data: Id={Id}, TripId={TripId}, Title={Title}", 
                        f.Id, f.Id, f.TripId, f.Title ?? "null");
                    // Продолжаем обработку остальных записей
                }
            }

            _logger.LogInformation("GetFlights: Successfully mapped {Count} flights to DTOs", flightDtos.Count);
            return Ok(flightDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFlights for tripId: {TripId}", tripId);
            throw; // Re-throw to let exception handler middleware handle it
        }
    }

    // GET: api/trips/{tripId}/flights/{flightId}
    [HttpGet("{flightId}")]
    public async Task<ActionResult<FlightDto>> GetFlight(int tripId, int flightId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var hasAccess = await _context.Memberships
            .AnyAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (!hasAccess)
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.Id == flightId && f.TripId == tripId);

        if (flight == null)
            return NotFound();

        var flightDto = new FlightDto
        {
            Id = flight.Id,
            TripId = flight.TripId,
            Category = (int)flight.Category,
            Type = (int)flight.Type,
            Title = flight.Title,
            Subtitle = flight.Subtitle,
            From = flight.From,
            To = flight.To,
            Date = flight.Date,
            Time = flight.Time.HasValue ? $"{flight.Time.Value.Hours:D2}:{flight.Time.Value.Minutes:D2}" : null,
            Status = flight.Status.HasValue ? (int)flight.Status.Value : null,
            Details = flight.Details,
            CreatedAt = flight.CreatedAt,
            UpdatedAt = flight.UpdatedAt
        };

        return Ok(flightDto);
    }

    // POST: api/trips/{tripId}/flights
    [HttpPost]
    public async Task<ActionResult<FlightDto>> CreateFlight(int tripId, CreateFlightDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут создавать)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify flights" });

        // Парсим время в формате "HH:mm" или "HH:mm:ss"
        TimeSpan? time = null;
        if (!string.IsNullOrEmpty(dto.Time))
        {
            // Пробуем разные форматы
            if (TimeSpan.TryParse(dto.Time, out var parsedTime))
            {
                time = parsedTime;
            }
            else if (dto.Time.Contains(':'))
            {
                // Парсим формат "HH:mm"
                var parts = dto.Time.Split(':');
                if (parts.Length >= 2 && 
                    int.TryParse(parts[0], out var hours) && 
                    int.TryParse(parts[1], out var minutes))
                {
                    time = new TimeSpan(hours, minutes, 0);
                }
            }
        }

        var flight = new Flight
        {
            TripId = tripId,
            Category = (BookingCategory)dto.Category,
            Type = (BookingType)dto.Type,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            From = dto.From,
            To = dto.To,
            Date = dto.Date,
            Time = time,
            Status = dto.Status.HasValue ? (BookingStatus)dto.Status.Value : null,
            Details = dto.Details,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        var flightDto = new FlightDto
        {
            Id = flight.Id,
            TripId = flight.TripId,
            Category = (int)flight.Category,
            Type = (int)flight.Type,
            Title = flight.Title,
            Subtitle = flight.Subtitle,
            From = flight.From,
            To = flight.To,
            Date = flight.Date,
            Time = flight.Time.HasValue ? $"{flight.Time.Value.Hours:D2}:{flight.Time.Value.Minutes:D2}" : null,
            Status = flight.Status.HasValue ? (int)flight.Status.Value : null,
            Details = flight.Details,
            CreatedAt = flight.CreatedAt,
            UpdatedAt = flight.UpdatedAt
        };

        return CreatedAtAction(nameof(GetFlight), new { tripId, flightId = flight.Id }, flightDto);
    }

    // PUT: api/trips/{tripId}/flights/{flightId}
    [HttpPut("{flightId}")]
    public async Task<ActionResult<FlightDto>> UpdateFlight(int tripId, int flightId, UpdateFlightDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут обновлять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify flights" });

        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.Id == flightId && f.TripId == tripId);

        if (flight == null)
            return NotFound();

        if (dto.Title != null)
            flight.Title = dto.Title;
        if (dto.Subtitle != null)
            flight.Subtitle = dto.Subtitle;
        if (dto.From != null)
            flight.From = dto.From;
        if (dto.To != null)
            flight.To = dto.To;
        if (dto.Date.HasValue)
            flight.Date = dto.Date.Value;
        if (dto.Time != null)
        {
            if (TimeSpan.TryParse(dto.Time, out var parsedTime))
                flight.Time = parsedTime;
        }
        if (dto.Status.HasValue)
            flight.Status = (BookingStatus)dto.Status.Value;
        if (dto.Details != null)
            flight.Details = dto.Details;
        flight.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var flightDto = new FlightDto
        {
            Id = flight.Id,
            TripId = flight.TripId,
            Category = (int)flight.Category,
            Type = (int)flight.Type,
            Title = flight.Title,
            Subtitle = flight.Subtitle,
            From = flight.From,
            To = flight.To,
            Date = flight.Date,
            Time = flight.Time.HasValue ? $"{flight.Time.Value.Hours:D2}:{flight.Time.Value.Minutes:D2}" : null,
            Status = flight.Status.HasValue ? (int)flight.Status.Value : null,
            Details = flight.Details,
            CreatedAt = flight.CreatedAt,
            UpdatedAt = flight.UpdatedAt
        };

        return Ok(flightDto);
    }

    // DELETE: api/trips/{tripId}/flights/{flightId}
    [HttpDelete("{flightId}")]
    public async Task<IActionResult> DeleteFlight(int tripId, int flightId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут удалять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify flights" });

        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.Id == flightId && f.TripId == tripId);

        if (flight == null)
            return NotFound();

        _context.Flights.Remove(flight);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
