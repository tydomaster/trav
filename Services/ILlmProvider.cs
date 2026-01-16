using TravelPlanner.Api.DTOs;

namespace TravelPlanner.Api.Services;

public interface ILlmProvider
{
    Task<AssistantMessageDto> SendMessageAsync(int tripId, string message, CancellationToken cancellationToken = default);
}
