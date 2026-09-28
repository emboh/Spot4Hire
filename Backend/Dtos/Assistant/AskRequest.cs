namespace Spot4Hire.Backend.Dtos.Assistant;

public class AskRequest
{
    public string Question { get; set; } = string.Empty;

    // The user's current position, sent by the browser only when the user
    // allows it. Used for "near me" questions; never shown to the model.
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
