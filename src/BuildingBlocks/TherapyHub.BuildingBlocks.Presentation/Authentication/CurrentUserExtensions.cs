using Microsoft.Extensions.DependencyInjection;

using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;

namespace TherapyHub.BuildingBlocks.Presentation.Authentication;

public static class CurrentUserExtensions
{
    /// <summary>Registers <see cref="ICurrentUser"/>, resolved from the token's "sub" claim of the current request.</summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}