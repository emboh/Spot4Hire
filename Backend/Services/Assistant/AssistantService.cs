using Microsoft.Extensions.AI;
using Spot4Hire.Backend.Dtos.Assistant;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Services.Assistant;

// Answers one question. The IChatClient pipeline (see Program.cs) already has
// UseFunctionInvocation, so a single GetResponseAsync call runs the whole loop:
// model asks for a tool -> tool runs -> result goes back -> model answers.
public sealed class AssistantService(
    IChatClient chat,
    AssistantTools tools,
    AssistantContext context,
    TimeProvider time) : IAssistantService
{
    public async Task<AskResponse> AskAsync(AskRequest request, CancellationToken ct)
    {
        // Set per-request facts before any tool can run.
        context.Latitude = request.Latitude;
        context.Longitude = request.Longitude;

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            Tools =
            [
                AIFunctionFactory.Create(tools.SearchVenues, "search_venues"),
                AIFunctionFactory.Create(tools.FindNearbyVenues, "find_nearby_venues"),
                AIFunctionFactory.Create(tools.GetVenueDetails, "get_venue_details"),
                AIFunctionFactory.Create(tools.GetUnits, "get_units"),
                AIFunctionFactory.Create(tools.CheckAvailability, "check_availability"),
            ],
        };

        List<ChatMessage> messages =
        [
            new(ChatRole.System, AssistantPrompts.System(time.GetUtcNow(), context.HasLocation)),
            new(ChatRole.User, request.Question),
        ];

        var response = await chat.GetResponseAsync(messages, options, ct);

        return new AskResponse { Answer = response.Text.Trim() };
    }
}
