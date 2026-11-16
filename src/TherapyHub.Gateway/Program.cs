using Microsoft.AspNetCore.Authentication.JwtBearer;

using TherapyHub.Gateway;
using TherapyHub.Gateway.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(GatewaySettings.ReverseProxySection));

// The gateway validates the same Keycloak token as the API, so the rate-limit scope (the subject) cannot be spoofed.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration[GatewaySettings.AuthorityKey];
        options.Audience = builder.Configuration[GatewaySettings.AudienceKey];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false; // Keep JWT claim names (e.g. "sub") as issued.
    });
builder.Services.AddAuthorization();

builder.Services.AddUserRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

// Order matters: authenticate first (to know the user), then rate limit (anonymous requests included,
// partitioned by IP), then authorize (reject anonymous requests on protected routes).
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();
app.MapReverseProxy();

app.Run();