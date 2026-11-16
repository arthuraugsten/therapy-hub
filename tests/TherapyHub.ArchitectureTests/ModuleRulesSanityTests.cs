using TherapyHub.ArchitectureTests.Infrastructure;

namespace TherapyHub.ArchitectureTests;

/// <summary>
/// Proves the generic module rules on fixture modules in this assembly (Fixtures/CrossModuleFixtures.cs). With only
/// one real module the cross-module rule passes trivially, so these tests show it would actually fail.
/// </summary>
public sealed class ModuleRulesSanityTests
{
    private static readonly System.Reflection.Assembly FixtureAssembly = typeof(ModuleRulesSanityTests).Assembly;

    private static readonly Layer FixtureApplication = new(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Application");

    private static readonly IReadOnlyList<Layer> FixtureLayers =
    [
        new(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Domain"),
        FixtureApplication,
        new(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Infrastructure"),
        new(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Api.Features"),
    ];

    [Fact]
    public void DiscoverModules_FixtureLayers_FindsAlphaAndBeta()
    {
        var modules = ModuleRules.DiscoverModules(FixtureLayers);

        Assert.Equal(["Alpha", "Beta"], modules.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void FindCrossModuleViolations_FixtureLayers_ReportsExactlyTheIllegalDependencies()
    {
        var violations = ModuleRules.FindCrossModuleViolations(FixtureLayers, FixtureApplication);

        Assert.Equal(
            [
                "TherapyHub.ArchitectureTests.Fixtures.Api.Features.Alpha.AlphaUsesBetaServiceInAsyncMethod -> Beta",
                "TherapyHub.ArchitectureTests.Fixtures.Infrastructure.Alpha.AlphaUsesBetaDomain -> Beta",
            ],
            violations.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void FindCrossModuleViolations_PublicApiLayerWithoutPublicApi_AlsoReportsThePublicApiUser()
    {
        // Same fixtures, but the exception points at a layer with no PublicApi namespace: the allowed dependency
        // on IBetaApi now counts too, which shows the exception is what keeps it legal.
        var noPublicApiLayer = new Layer(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Domain");

        var violations = ModuleRules.FindCrossModuleViolations(FixtureLayers, noPublicApiLayer);

        Assert.Contains(
            "TherapyHub.ArchitectureTests.Fixtures.Application.Alpha.AlphaUsesBetaPublicApi -> Beta",
            violations);
    }

    [Fact]
    public void HaveDependencyOn_AssemblyThatUsesEfCore_Fails()
    {
        var result = NetArchTest.Rules.Types.InAssembly(ArchitectureAssemblies.Infrastructure)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.NotEmpty(ModuleRules.FailingTypeNames(result));
    }

    [Fact]
    public void FindTypesOutsideModules_TypeOutsideModuleNamespace_IsReported()
    {
        var layer = new Layer(FixtureAssembly, "TherapyHub.ArchitectureTests.Fixtures.Application");
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        var outside = ModuleRules.FindTypesOutsideModules(layer, allowed);

        Assert.Contains(typeof(ModuleRulesSanityTests).FullName, outside);
        Assert.DoesNotContain(
            "TherapyHub.ArchitectureTests.Fixtures.Application.Beta.BetaService",
            outside);
    }
}