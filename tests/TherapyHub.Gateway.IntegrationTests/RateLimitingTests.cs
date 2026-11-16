using System.Net;

using TherapyHub.Gateway.IntegrationTests.Infrastructure;
using TherapyHub.Gateway.RateLimiting;

namespace TherapyHub.Gateway.IntegrationTests;

public sealed class RateLimitingTests(GatewayFactory factory) : IClassFixture<GatewayFactory>
{
    // Any path the catch-all route forwards; the gateway doesn't know API routes on purpose.
    private const string ProxiedPath = "/patients";
    private const string ProxiedPathWithId = ProxiedPath + "/123";
    private const string HealthPath = "/health";

    private static readonly TimeSpan MinimumRetryAfter = TimeSpan.FromSeconds(1);

    /// <summary>The requirement: 30 requests per user, resetting every 10 seconds (appsettings.json must match).</summary>
    [Fact]
    public void Configuration_UsesRequiredLimits()
    {
        Assert.Equal(UserRateLimitOptions.DefaultPermitLimit, factory.RateLimits.PermitLimit);
        Assert.Equal(UserRateLimitOptions.DefaultWindowSeconds, factory.RateLimits.WindowSeconds);
    }

    [Fact]
    public async Task Proxy_AuthenticatedRequest_IsForwardedToBackend()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());

        var response = await client.GetAsync(ProxiedPathWithId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProxiedPathWithId, Assert.Single(response.Headers.GetValues(GatewayFactory.BackendHeader)));
    }

    [Fact]
    public async Task Proxy_AnonymousRequest_Returns401WithoutReachingBackend()
    {
        var response = await factory.CreateClient().GetAsync(ProxiedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains(GatewayFactory.BackendHeader));
    }

    [Fact]
    public async Task Proxy_RequestsBeyondPermitLimitInWindow_Returns429WithRetryAfter()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());

        for (var i = 0; i < factory.RateLimits.PermitLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(ProxiedPath)).StatusCode);
        }

        var rejected = await client.GetAsync(ProxiedPath);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter?.Delta);
        Assert.InRange(
            rejected.Headers.RetryAfter.Delta.Value,
            MinimumRetryAfter,
            TimeSpan.FromSeconds(factory.RateLimits.WindowSeconds));
        Assert.False(rejected.Headers.Contains(GatewayFactory.BackendHeader));
    }

    [Fact]
    public async Task Proxy_LimitIsScopedPerUser_OtherUserIsNotAffected()
    {
        var exhausted = factory.CreateClientFor(Guid.NewGuid());
        for (var i = 0; i <= factory.RateLimits.PermitLimit; i++)
        {
            await exhausted.GetAsync(ProxiedPath);
        }

        var otherUser = factory.CreateClientFor(Guid.NewGuid());
        var response = await otherUser.GetAsync(ProxiedPath);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await exhausted.GetAsync(ProxiedPath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_IsAnonymousAndNotRateLimited()
    {
        var client = factory.CreateClient();

        for (var i = 0; i <= factory.RateLimits.PermitLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(HealthPath)).StatusCode);
        }
    }
}