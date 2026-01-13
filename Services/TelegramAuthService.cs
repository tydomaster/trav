using System.Security.Cryptography;
using System.Text;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Services;

public interface ITelegramAuthService
{
    bool ValidateInitData(string initData, string secretKey);
    TelegramUserData? ParseInitData(string initData);
}

public class TelegramUserData
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? PhotoUrl { get; set; }
}

public class TelegramAuthService : ITelegramAuthService
{
    public bool ValidateInitData(string initData, string secretKey)
    {
        if (string.IsNullOrEmpty(initData) || string.IsNullOrEmpty(secretKey))
            return false;

        try
        {
            var parameters = ParseQueryString(initData);
            if (!parameters.TryGetValue("hash", out var hash) || string.IsNullOrEmpty(hash))
                return false;

            parameters.Remove("hash");

            // Создаем data-check-string
            var dataCheckString = string.Join("\n", 
                parameters.OrderBy(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            // Вычисляем секретный ключ
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataCheckStringBytes = Encoding.UTF8.GetBytes(dataCheckString);
            
            using var hmac = new HMACSHA256(secretKeyBytes);
            var computedHash = hmac.ComputeHash(dataCheckStringBytes);
            var computedHashString = BitConverter.ToString(computedHash)
                .Replace("-", "")
                .ToLower();

            return computedHashString == hash.ToLower();
        }
        catch
        {
            return false;
        }
    }

    public TelegramUserData? ParseInitData(string initData)
    {
        if (string.IsNullOrEmpty(initData))
            return null;

        try
        {
            var parameters = ParseQueryString(initData);
            if (!parameters.TryGetValue("user", out var userParam) || string.IsNullOrEmpty(userParam))
                return null;

            var userJson = Uri.UnescapeDataString(userParam);
            var user = System.Text.Json.JsonSerializer.Deserialize<TelegramUserData>(userJson);

            return user;
        }
        catch
        {
            return null;
        }
    }

    private Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();
        var pairs = queryString.Split('&');
        
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }
        
        return result;
    }
}

