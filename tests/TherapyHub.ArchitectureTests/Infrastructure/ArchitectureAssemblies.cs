using System.Reflection;

namespace TherapyHub.ArchitectureTests.Infrastructure;

/// <summary>A layer project and the namespace under which its modules live (the module is the next segment).</summary>
public sealed record Layer(Assembly Assembly, string ModuleRoot);

/// <summary>
/// The assemblies under test, loaded by name. The Api and the Gateway both generate a global <c>Program</c>, so no
/// type is named here; loading by name also keeps this file free of module-specific types.
/// </summary>
public static class ArchitectureAssemblies
{
    public const string ProductPrefix = "TherapyHub.";

    public static readonly Assembly BuildingBlocksDomain = Assembly.Load("TherapyHub.BuildingBlocks.Domain");
    public static readonly Assembly BuildingBlocksApplication = Assembly.Load("TherapyHub.BuildingBlocks.Application");
    public static readonly Assembly BuildingBlocksInfrastructure = Assembly.Load("TherapyHub.BuildingBlocks.Infrastructure");
    public static readonly Assembly BuildingBlocksPresentation = Assembly.Load("TherapyHub.BuildingBlocks.Presentation");
    public static readonly Assembly Domain = Assembly.Load("TherapyHub.Domain");
    public static readonly Assembly Application = Assembly.Load("TherapyHub.Application");
    public static readonly Assembly Infrastructure = Assembly.Load("TherapyHub.Infrastructure");
    public static readonly Assembly Api = Assembly.Load("TherapyHub.Api");
    public static readonly Assembly Gateway = Assembly.Load("TherapyHub.Gateway");

    public static readonly Layer DomainLayer = new(Domain, "TherapyHub.Domain");
    public static readonly Layer ApplicationLayer = new(Application, "TherapyHub.Application");
    public static readonly Layer InfrastructureLayer = new(Infrastructure, "TherapyHub.Infrastructure");
    public static readonly Layer ApiLayer = new(Api, "TherapyHub.Api.Features");

    public static readonly IReadOnlyList<Layer> Layers = [DomainLayer, ApplicationLayer, InfrastructureLayer, ApiLayer];

    /// <summary>Types allowed outside module namespaces in the layer projects.</summary>
    public static readonly IReadOnlySet<string> CompositionRoots = new HashSet<string>(StringComparer.Ordinal)
    {
        "TherapyHub.Application.DependencyInjection",
        "TherapyHub.Infrastructure.DependencyInjection",
        "TherapyHub.Api.Authentication.AuthenticationSettings",
        "Program",
    };

    public static Assembly ByName(string assemblyName) => Assembly.Load(assemblyName);

    public static Layer LayerByAssemblyName(string assemblyName) =>
        Layers.Single(layer => layer.Assembly.GetName().Name == assemblyName);
}