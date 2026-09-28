namespace Spot4Hire.Backend.Services.Assistant;

// Facts about the caller for the current request. Registered as scoped, so
// each HTTP request gets its own instance.
//
// Tools read these values from here instead of taking them as parameters, so
// the model can never invent or change them (and they stay out of the traces).
public sealed class AssistantContext
{
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
}
