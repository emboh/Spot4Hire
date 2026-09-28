namespace Spot4Hire.Backend.Common;

// Named rate limiter policies, used with [EnableRateLimiting(...)].
public static class RateLimitPolicies
{
    // LLM calls are slow and costly, so the assistant gets a tighter limit
    // than the global 100/minute.
    public const string Assistant = "assistant";
}
