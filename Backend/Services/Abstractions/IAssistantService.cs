using Spot4Hire.Backend.Dtos.Assistant;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IAssistantService
{
    Task<AskResponse> AskAsync(AskRequest request, CancellationToken ct);
}
