using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

using TherapyHub.Gateway.RateLimiting;

using Yarp.ReverseProxy.Forwarder;

namespace TherapyHub.Gateway.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the gateway with a stub backend (no real API) and a test authentication scheme (no Keycloak).
/// </summary>
public sealed class GatewayFactory : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";
    public const string BackendHeader = "X-Stub-Backend";

    private const string StubBackendAddress = "http://backend.test";
    private const string StubBackendBody = "ok";

    /// <summary>The limits the gateway is actually running with (from its configuration).</summary>
    public UserRateLimitOptions RateLimits => Services.GetRequiredService<IOptions<UserRateLimitOptions>>().Value;

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestingEnvironment);
        builder.UseSetting(GatewaySettings.ApiDestinationAddressKey, StubBackendAddress);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory, StubBackendClientFactory>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private sealed class StubBackendClientFactory : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => new(new StubBackendHandler());
    }

    /// <summary>Stands in for the API: every forwarded request gets 200 and a marker header.</summary>
    private sealed class StubBackendHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(StubBackendBody) };
            response.Headers.Add(BackendHeader, request.RequestUri?.AbsolutePath);
            return Task.FromResult(response);
        }
    }
}

/// <summary>Authenticates requests that carry the <see cref="UserHeader"/> header, issuing it as the subject claim.</summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}