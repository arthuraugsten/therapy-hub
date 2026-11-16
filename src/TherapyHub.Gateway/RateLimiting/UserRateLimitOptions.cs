namespace TherapyHub.Gateway.RateLimiting;

/// <summary>Fixed-window limit applied per authenticated user (or per IP for anonymous requests).</summary>
public sealed class UserRateLimitOptions
{
    public const string SectionName = "RateLimiting:PerUser";

    public const int DefaultPermitLimit = 30;
    public const int DefaultWindowSeconds = 10;

    /// <summary>Requests allowed in each window.</summary>
    public int PermitLimit { get; init; } = DefaultPermitLimit;

    /// <summary>Window length; the counter resets when it ends.</summary>
    public int WindowSeconds { get; init; } = DefaultWindowSeconds;
}