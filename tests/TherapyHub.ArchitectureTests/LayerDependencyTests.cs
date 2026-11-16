using NetArchTest.Rules;

using TherapyHub.ArchitectureTests.Infrastructure;

namespace TherapyHub.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string ApiFeaturesPattern = @"^TherapyHub\.Api\.Features(\..+)?$";

    [Theory]
    [InlineData("TherapyHub.BuildingBlocks.Domain")]
    [InlineData("TherapyHub.Domain")]
    public void DomainAssembly_Types_DoNotDependOnEfCoreOrAspNetCore(string assemblyName)
    {
        var assembly = ArchitectureAssemblies.ByName(assemblyName);

        var result = Types.InAssembly(assembly)
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    [Theory]
    [InlineData("TherapyHub.BuildingBlocks.Application")]
    [InlineData("TherapyHub.Application")]
    public void ApplicationAssembly_Types_DoNotDependOnEfCore(string assemblyName)
    {
        var assembly = ArchitectureAssemblies.ByName(assemblyName);

        var result = Types.InAssembly(assembly)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    // Guards the rule below against a namespace predicate that silently selects nothing.
    [Fact]
    public void ApiFeatures_NamespacePredicate_SelectsTypes()
    {
        var types = Types.InAssembly(ArchitectureAssemblies.Api)
            .That().ResideInNamespaceMatching(ApiFeaturesPattern)
            .GetTypes();

        Assert.NotEmpty(types);
    }

    [Fact]
    public void ApiFeatures_Types_DoNotDependOnEfCoreOrInfrastructure()
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Api)
            .That().ResideInNamespaceMatching(ApiFeaturesPattern)
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "TherapyHub.Infrastructure")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    [Fact]
    public void Gateway_ReferencedAssemblies_ContainNoTherapyHubAssembly()
    {
        var references = ArchitectureAssemblies.Gateway.GetReferencedAssemblies()
            .Select(static reference => reference.Name)
            .Where(static name => name?.StartsWith(ArchitectureAssemblies.ProductPrefix, StringComparison.Ordinal) ?? false);

        Assert.Empty(references);
    }

    [Fact]
    public void Gateway_Types_DoNotDependOnOtherTherapyHubProjects()
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Gateway)
            .ShouldNot().HaveDependencyOnAny(
                "TherapyHub.BuildingBlocks",
                "TherapyHub.Domain",
                "TherapyHub.Application",
                "TherapyHub.Infrastructure",
                "TherapyHub.Api")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }
}