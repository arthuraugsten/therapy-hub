// Fixture modules "Alpha" and "Beta" laid out like the real layers, used only by ModuleRulesSanityTests to prove that
// the cross-module rule detects violations. Nothing here is production code.

namespace TherapyHub.ArchitectureTests.Fixtures.Domain.Beta
{
    public sealed class BetaThing;
}

namespace TherapyHub.ArchitectureTests.Fixtures.Application.Beta.PublicApi
{
    public interface IBetaApi
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    }
}

namespace TherapyHub.ArchitectureTests.Fixtures.Application.Beta
{
    public sealed class BetaService
    {
        public bool IsReady() => true;
    }
}

namespace TherapyHub.ArchitectureTests.Fixtures.Domain.Alpha
{
    public sealed class AlphaThing;
}

namespace TherapyHub.ArchitectureTests.Fixtures.Application.Alpha
{
    using TherapyHub.ArchitectureTests.Fixtures.Application.Beta.PublicApi;
    using TherapyHub.ArchitectureTests.Fixtures.Domain.Alpha;

    // Allowed: own module plus the other module's public API.
    public sealed class AlphaUsesBetaPublicApi(IBetaApi betaApi)
    {
        public AlphaThing Thing { get; } = new();

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
            betaApi.ExistsAsync(id, cancellationToken);
    }
}

namespace TherapyHub.ArchitectureTests.Fixtures.Infrastructure.Alpha
{
    using TherapyHub.ArchitectureTests.Fixtures.Domain.Beta;

    // Violation: a member signature uses another module's domain type.
    public sealed class AlphaUsesBetaDomain
    {
        public BetaThing? Thing { get; set; }
    }
}

namespace TherapyHub.ArchitectureTests.Fixtures.Api.Features.Alpha
{
    using TherapyHub.ArchitectureTests.Fixtures.Application.Beta;

    // Violation hidden in an async method body (lives in a compiler-generated nested state machine).
    public sealed class AlphaUsesBetaServiceInAsyncMethod
    {
        public async Task<bool> IsReadyAsync()
        {
            await Task.Yield();
            var service = new BetaService();
            return service.IsReady();
        }
    }
}