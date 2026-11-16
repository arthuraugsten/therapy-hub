using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;
using TherapyHub.BuildingBlocks.Domain.Common;

namespace TherapyHub.BuildingBlocks.Presentation.Authentication;

/// <summary>Resolves the psychologist from the token's subject claim (the Keycloak user ID).</summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public PsychologistId PsychologistId
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subject, out var id)
                ? new PsychologistId(id)
                : throw new InvalidOperationException(
                    "No authenticated psychologist. Did the endpoint forget RequireAuthorization()?");
        }
    }
}