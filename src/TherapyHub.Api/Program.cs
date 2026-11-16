using FluentValidation;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using TherapyHub.Api.Authentication;
using TherapyHub.Application;
using TherapyHub.BuildingBlocks.Presentation.Authentication;
using TherapyHub.BuildingBlocks.Presentation.Endpoints;
using TherapyHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);
builder.Services.AddEndpoints(typeof(Program).Assembly);

builder.Services.AddCurrentUser();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keycloak realm, e.g. http://localhost:8080/realms/therapy-hub. Standard OIDC only, no Keycloak SDK.
        options.Authority = builder.Configuration[AuthenticationSettings.AuthorityKey];
        options.Audience = builder.Configuration[AuthenticationSettings.AudienceKey];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false; // Keep JWT claim names (e.g. "sub") as issued.
    });
builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapEndpoints();

app.Run();