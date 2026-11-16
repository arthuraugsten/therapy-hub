using NetArchTest.Rules;

using TherapyHub.ArchitectureTests.Infrastructure;

namespace TherapyHub.ArchitectureTests;

public sealed class BuildingBlocksTests
{
    [Theory]
    [InlineData("TherapyHub.BuildingBlocks.Domain")]
    [InlineData("TherapyHub.BuildingBlocks.Application")]
    [InlineData("TherapyHub.BuildingBlocks.Infrastructure")]
    [InlineData("TherapyHub.BuildingBlocks.Presentation")]
    public void BuildingBlocks_AnyAssembly_DoesNotDependOnLayerProjects(string assemblyName)
    {
        var assembly = ArchitectureAssemblies.ByName(assemblyName);

        var result = Types.InAssembly(assembly)
            .ShouldNot().HaveDependencyOnAny(
                "TherapyHub.Domain",
                "TherapyHub.Application",
                "TherapyHub.Infrastructure",
                "TherapyHub.Api")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    [Fact]
    public void BuildingBlocksDomain_ReferencedAssemblies_ContainNoOtherTherapyHubAssembly()
    {
        var references = ArchitectureAssemblies.BuildingBlocksDomain.GetReferencedAssemblies()
            .Select(static reference => reference.Name)
            .Where(static name => name?.StartsWith(ArchitectureAssemblies.ProductPrefix, StringComparison.Ordinal) ?? false);

        Assert.Empty(references);
    }

    [Fact]
    public void BuildingBlocksDomain_Types_DoNotDependOnOtherTherapyHubProjects()
    {
        var result = Types.InAssembly(ArchitectureAssemblies.BuildingBlocksDomain)
            .ShouldNot().HaveDependencyOnAny(
                "TherapyHub.BuildingBlocks.Application",
                "TherapyHub.BuildingBlocks.Infrastructure",
                "TherapyHub.BuildingBlocks.Presentation",
                "TherapyHub.Domain",
                "TherapyHub.Application",
                "TherapyHub.Infrastructure",
                "TherapyHub.Api",
                "TherapyHub.Gateway")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    [Fact]
    public void BuildingBlocksApplication_Types_DoNotDependOnInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(ArchitectureAssemblies.BuildingBlocksApplication)
            .ShouldNot().HaveDependencyOnAny(
                "TherapyHub.BuildingBlocks.Infrastructure",
                "TherapyHub.BuildingBlocks.Presentation")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }
}