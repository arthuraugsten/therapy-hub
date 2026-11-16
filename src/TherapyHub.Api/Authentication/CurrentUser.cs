using Microsoft.IdentityModel.JsonWebTokens;

using TherapyHub.Application.Abstractions.Authentication;
using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Api.Authentication;

/// <summary>Resolves the psychologist from the token's subject claim (the Keycloak user ID).</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public PsychologistId PsychologistId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subject, out var id)
                ? new PsychologistId(id)
                : throw new InvalidOperationException(
                    "No authenticated psychologist. Did the endpoint forget RequireAuthorization()?");
        }
    }
}