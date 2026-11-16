using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TherapyHub.Gateway.RateLimiting;

public static class UserRateLimiting
{
    /// <summary>Referenced by YARP routes through "RateLimiterPolicy" in configuration.</summary>
    public const string PolicyName = "per-user";

    public const string RejectionTitle = "Gateway.TooManyRequests";

    // Partition keys: one counter per user, or per IP when the request is anonymous.
    private const string UserPartitionPrefix = "user:";
    private const string IpPartitionPrefix = "ip:";
    private const string UnknownIp = "unknown";

    /// <summary>Requests over the limit are rejected immediately, never queued.</summary>
    private const int NoQueue = 0;

    public static IServiceCollection AddUserRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UserRateLimitOptions>()
            .Bind(configuration.GetSection(UserRateLimitOptions.SectionName))
            .Validate(o => o.PermitLimit > 0 && o.WindowSeconds > 0, "Rate limit values must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(PolicyName, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<UserRateLimitOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = NoQueue,
                        AutoReplenishment = true,
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// The current user (subject of the validated Keycloak token) is the limit's scope.
    /// Anonymous requests fall back to the client IP so they cannot bypass the limit.
    /// </summary>
    internal static string GetPartitionKey(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(subject)
            ? UserPartitionPrefix + subject
            : IpPartitionPrefix + (httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIp);
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await TypedResults.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: RejectionTitle,
                detail: "Too many requests. Try again after the time in the Retry-After header.")
            .ExecuteAsync(context.HttpContext);
    }
}