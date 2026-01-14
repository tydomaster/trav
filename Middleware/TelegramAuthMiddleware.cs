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

        // Логируем для отладки
        logger.LogInformation("Auth check - Path: {Path}, HasInitData: {HasInitData}, IsDevelopment: {IsDev}", 
            path, !string.IsNullOrEmpty(initData), _isDevelopment);

        if (!string.IsNullOrEmpty(initData))
        {
            // Валидация initData
            // Новый метод: использует Ed25519 с публичным ключом Telegram (не требует Secret Key)
            // Старый метод: использует HMAC-SHA256 с Secret Key (для обратной совместимости)
            var secretKey = _configuration["Telegram:BotSecretKey"] ?? "";
            var hasSecretKey = !string.IsNullOrEmpty(secretKey);
            
            logger.LogInformation("Validating initData - HasSecretKey: {HasKey}, SecretKeyLength: {KeyLength}", 
                hasSecretKey, secretKey.Length);
            
            // ValidateInitData теперь работает без secretKey для нового метода Ed25519
            var isValid = _isDevelopment || authService.ValidateInitData(initData, hasSecretKey ? secretKey : null, logger);
            
            logger.LogInformation("InitData validation result: {IsValid}", isValid);

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
                logger.LogWarning("Invalid initData received. IsDevelopment: {IsDev}, HasSecretKey: {HasKey}, SecretKeyLength: {KeyLength}", 
                    _isDevelopment, !string.IsNullOrEmpty(secretKey), secretKey.Length);
                
                if (!_isDevelopment)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Unauthorized", 
                        message = "Invalid Telegram initData. Please ensure BotSecretKey is configured correctly in Railway.",
                        hasSecretKey = !string.IsNullOrEmpty(secretKey)
                    });
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
                logger.LogWarning("No initData provided in production mode. Path: {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { 
                    error = "Unauthorized", 
                    message = "Telegram initData is required. Please ensure you are opening the app from Telegram Mini App.",
                    hint = "Check that window.Telegram.WebApp.initData is available in the browser console"
                });
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

