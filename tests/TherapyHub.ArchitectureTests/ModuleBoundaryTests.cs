using NetArchTest.Rules;

using TherapyHub.ArchitectureTests.Infrastructure;

namespace TherapyHub.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    private const string PublicApiPattern = $@"\.{ModuleRules.PublicApiSegment}(\..+)?$";

    // Guards every generic rule below: with an empty module set they would pass without checking anything.
    [Theory]
    [InlineData("TherapyHub.Domain")]
    [InlineData("TherapyHub.Application")]
    [InlineData("TherapyHub.Infrastructure")]
    [InlineData("TherapyHub.Api")]
    public void DiscoverModules_LayerAssembly_FindsPatients(string assemblyName)
    {
        var layer = ArchitectureAssemblies.LayerByAssemblyName(assemblyName);

        var modules = ModuleRules.DiscoverModules(layer);

        Assert.Contains("Patients", modules);
    }

    [Fact]
    public void Modules_AnyLayer_DoNotDependOnOtherModulesExceptPublicApi()
    {
        var violations = ModuleRules.FindCrossModuleViolations(
            ArchitectureAssemblies.Layers,
            ArchitectureAssemblies.ApplicationLayer);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("TherapyHub.Domain")]
    [InlineData("TherapyHub.Application")]
    [InlineData("TherapyHub.Infrastructure")]
    [InlineData("TherapyHub.Api")]
    public void LayerAssembly_Types_ResideInModuleNamespacesOrAreCompositionRoots(string assemblyName)
    {
        var layer = ArchitectureAssemblies.LayerByAssemblyName(assemblyName);

        var outside = ModuleRules.FindTypesOutsideModules(layer, ArchitectureAssemblies.CompositionRoots);

        Assert.Empty(outside);
    }

    [Fact]
    public void PublicApi_Namespaces_AreFoundForPatients()
    {
        var namespaces = PublicApiTypes().Select(static type => type.Namespace).Distinct();

        Assert.Contains("TherapyHub.Application.Patients.PublicApi", namespaces);
    }

    [Fact]
    public void PublicApi_Types_AreInterfacesOrRecords()
    {
        var offending = PublicApiTypes()
            .Where(static type => !type.IsInterface && !IsRecordClass(type))
            .Select(static type => type.FullName);

        Assert.Empty(offending);
    }

    // Guards the rule below against a namespace predicate that silently selects nothing.
    [Fact]
    public void PublicApi_NamespacePredicate_SelectsTypes()
    {
        var types = Types.InAssemblies(ArchitectureAssemblies.Layers.Select(static layer => layer.Assembly))
            .That().ResideInNamespaceMatching(PublicApiPattern)
            .GetTypes();

        Assert.NotEmpty(types);
    }

    [Fact]
    public void PublicApi_Types_DoNotDependOnDomainEfCoreOrAspNetCore()
    {
        var result = Types.InAssemblies(ArchitectureAssemblies.Layers.Select(static layer => layer.Assembly))
            .That().ResideInNamespaceMatching(PublicApiPattern)
            .ShouldNot().HaveDependencyOnAny(
                "TherapyHub.Domain",
                "TherapyHub.BuildingBlocks.Domain",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.Empty(ModuleRules.FailingTypeNames(result));
    }

    private static IEnumerable<Type> PublicApiTypes() =>
        ArchitectureAssemblies.Layers
            .SelectMany(static layer => ModuleRules.SourceTypes(layer.Assembly))
            .Where(static type => type.Namespace?.Split('.').Contains(ModuleRules.PublicApiSegment) ?? false);

    // Record classes get a compiler-generated "<Clone>$" method; ordinary classes cannot declare that name.
    private static bool IsRecordClass(Type type) => type.IsClass && type.GetMethod("<Clone>$") is not null;
}