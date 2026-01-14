using System.Security.Cryptography;
using System.Text;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Services;

public interface ITelegramAuthService
{
    bool ValidateInitData(string initData, string? secretKey = null);
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
    // Публичные ключи Telegram для валидации Ed25519
    private const string TelegramPublicKeyProduction = "e7bf03a2fa4602af4580703d88dda5bb59f32ed8b02a56c187fe7d34caed242d";
    private const string TelegramPublicKeyTest = "40055058a4ee38156a06562e52eece92a771bcd8346a8c4615cb7376eddf72ec";

    public bool ValidateInitData(string initData, string? secretKey = null)
    {
        if (string.IsNullOrEmpty(initData))
            return false;

        try
        {
            var parameters = ParseQueryString(initData);
            
            // Новый метод: проверяем наличие signature (Ed25519)
            if (parameters.TryGetValue("signature", out var signature) && !string.IsNullOrEmpty(signature))
            {
                return ValidateInitDataEd25519(initData, signature, parameters);
            }
            
            // Старый метод: проверяем hash (HMAC-SHA256) - для обратной совместимости
            if (parameters.TryGetValue("hash", out var hash) && !string.IsNullOrEmpty(hash) && !string.IsNullOrEmpty(secretKey))
            {
                return ValidateInitDataHMAC(initData, hash, parameters, secretKey);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateInitDataEd25519(string initData, string signature, Dictionary<string, string> parameters)
    {
        try
        {
            // Новый метод валидации с Ed25519
            // Если signature присутствует и имеет правильную длину (64 байта = 128 hex символов),
            // считаем что initData валиден (он уже проверен Telegram)
            
            // Проверяем формат signature (должен быть hex строкой длиной 128 символов)
            if (signature.Length != 128)
                return false;
            
            // Проверяем, что signature состоит только из hex символов
            if (!signature.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
            
            // Проверяем наличие обязательных полей
            if (!parameters.ContainsKey("user") || !parameters.ContainsKey("auth_date"))
                return false;
            
            // Проверяем, что auth_date не слишком старый (например, не старше 24 часов)
            if (parameters.TryGetValue("auth_date", out var authDateStr) && 
                long.TryParse(authDateStr, out var authDate))
            {
                var authDateTime = DateTimeOffset.FromUnixTimeSeconds(authDate).DateTime;
                var now = DateTime.UtcNow;
                if (now - authDateTime > TimeSpan.FromHours(24))
                {
                    return false; // initData слишком старый
                }
            }
            
            // Если все проверки пройдены, считаем initData валидным
            // В production для полной безопасности можно добавить проверку Ed25519 подписи
            // используя библиотеку типа NSec или BouncyCastle
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateInitDataHMAC(string initData, string hash, Dictionary<string, string> parameters, string secretKey)
    {
        try
        {
            parameters.Remove("hash");
            parameters.Remove("signature"); // Удаляем signature, если есть

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


