using TravelPlanner.Api.DTOs;

namespace TravelPlanner.Api.Services;

public class MockLlmProvider : ILlmProvider
{
    public async Task<AssistantMessageDto> SendMessageAsync(int tripId, string message, CancellationToken cancellationToken = default)
    {
        // Симуляция задержки
        await Task.Delay(1500, cancellationToken);

        // Простые эвристики для демонстрации
        var suggestions = new List<AssistantSuggestionDto>();
        
        if (message.ToLower().Contains("план") || message.ToLower().Contains("маршрут"))
        {
            suggestions.Add(new AssistantSuggestionDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Маршрут на День 1",
                Description = "Голубая мечеть → Собор Святой Софии → Цистерна Базилика"
            });
        }
        
        if (message.ToLower().Contains("еда") || message.ToLower().Contains("обед") || message.ToLower().Contains("ресторан"))
        {
            suggestions.Add(new AssistantSuggestionDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Места для обеда",
                Description = "3 ресторана рядом с Sultanahmet"
            });
        }
        
        if (message.ToLower().Contains("packing") || message.ToLower().Contains("собрать") || message.ToLower().Contains("вещи"))
        {
            suggestions.Add(new AssistantSuggestionDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Packing List",
                Description = "Паспорт, зарядки, лекарства, адаптер"
            });
        }

        var response = new AssistantMessageDto
        {
            Id = Guid.NewGuid().ToString(),
            Type = "assistant",
            Content = suggestions.Any() 
                ? "Я подготовил несколько предложений для вашего маршрута:" 
                : "Понял! Чем еще могу помочь?",
            Suggestions = suggestions.Any() ? suggestions : null
        };

        return response;
    }
}
