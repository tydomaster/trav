using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.Models;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Middleware;

public class TelegramAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly bool _isDevelopment;

    public TelegramAuthMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env)
    {
        _next = next;
        _configuration = configuration;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context, ITelegramAuthService authService, ApplicationDbContext dbContext)
    {
        // Пропускаем авторизацию для некоторых endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/api/telegram/validate"))
        {
            await _next(context);
            return;
        }

        var initData = context.Request.Headers["X-Telegram-Init-Data"].FirstOrDefault() 
            ?? context.Request.Query["initData"].FirstOrDefault();

        var logger = context.RequestServices.GetRequiredService<ILogger<TelegramAuthMiddleware>>();
        User? user = null;

        if (!string.IsNullOrEmpty(initData))
        {
            // Валидация initData
            var secretKey = _configuration["Telegram:BotSecretKey"] ?? "";
            var isValid = _isDevelopment || authService.ValidateInitData(initData, secretKey);

            if (isValid)
            {
                var userData = authService.ParseInitData(initData);
                if (userData != null)
                {
                    try
                    {
                        logger.LogInformation("Authenticating user with TelegramId: {TelegramId}", userData.Id);
                        
                        // Получаем или создаем пользователя
                        user = await dbContext.Users
                            .FirstOrDefaultAsync(u => u.TelegramId == userData.Id);

                        if (user == null)
                        {
                            user = new User
                            {
                                TelegramId = userData.Id,
                                Name = $"{userData.FirstName} {userData.LastName}".Trim(),
                                Avatar = userData.PhotoUrl,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            dbContext.Users.Add(user);
                            await dbContext.SaveChangesAsync();
                            logger.LogInformation("Created new user with TelegramId: {TelegramId}, UserId: {UserId}", userData.Id, user.Id);
                        }
                        else
                        {
                            // Обновляем данные пользователя
                            user.Name = $"{userData.FirstName} {userData.LastName}".Trim();
                            if (!string.IsNullOrEmpty(userData.PhotoUrl))
                                user.Avatar = userData.PhotoUrl;
                            user.UpdatedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                            logger.LogInformation("Updated user with TelegramId: {TelegramId}, UserId: {UserId}", userData.Id, user.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating/updating user from initData");
                    }
                }
                else
                {
                    logger.LogWarning("Failed to parse user data from initData");
                }
            }
            else
            {
                // В production невалидный initData = 401
                logger.LogWarning("Invalid initData received. IsDevelopment: {IsDev}, HasSecretKey: {HasKey}", 
                    _isDevelopment, !string.IsNullOrEmpty(secretKey));
                
                if (!_isDevelopment)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid or missing Telegram initData");
                    return;
                }
            }
        }
        else
        {
            // Если initData нет
            if (_isDevelopment)
            {
                // В development используем мок-пользователя
                var mockTelegramId = long.Parse(_configuration["Dev:MockTelegramId"] ?? "123456789");
                logger.LogWarning("No initData provided, using mock user with TelegramId: {TelegramId}", mockTelegramId);
                
                user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.TelegramId == mockTelegramId);

                if (user == null)
                {
                    user = new User
                    {
                        TelegramId = mockTelegramId,
                        Name = "Test User",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                }
            }
            else
            {
                // В production отсутствие initData = 401
                logger.LogWarning("No initData provided in production mode");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Telegram initData is required");
                return;
            }
        }

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("TelegramId", user.TelegramId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Telegram");
            context.User = new ClaimsPrincipal(identity);
            context.Items["CurrentUser"] = user;
        }

        await _next(context);
    }
}

public static class TelegramAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseTelegramAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TelegramAuthMiddleware>();
    }
}

