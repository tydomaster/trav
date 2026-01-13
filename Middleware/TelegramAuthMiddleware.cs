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
                        }
                        else
                        {
                            // Обновляем данные пользователя
                            user.Name = $"{userData.FirstName} {userData.LastName}".Trim();
                            if (!string.IsNullOrEmpty(userData.PhotoUrl))
                                user.Avatar = userData.PhotoUrl;
                            user.UpdatedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но продолжаем выполнение
                        var logger = context.RequestServices.GetRequiredService<ILogger<TelegramAuthMiddleware>>();
                        logger.LogError(ex, "Error creating/updating user from initData");
                    }
                }
            }
            else
            {
                // Логируем неудачную валидацию
                var logger = context.RequestServices.GetRequiredService<ILogger<TelegramAuthMiddleware>>();
                logger.LogWarning("Invalid initData received. IsDevelopment: {IsDev}, HasSecretKey: {HasKey}", 
                    _isDevelopment, !string.IsNullOrEmpty(secretKey));
            }
        }
        else
        {
            // Если initData нет, используем мок-пользователя (для разработки)
            // В production это должно быть запрещено, но для локальной разработки разрешаем
            var mockTelegramId = long.Parse(_configuration["Dev:MockTelegramId"] ?? "123456789");
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

