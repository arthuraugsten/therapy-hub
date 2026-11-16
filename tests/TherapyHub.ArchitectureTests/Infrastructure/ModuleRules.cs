using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using NetArchTest.Rules;

namespace TherapyHub.ArchitectureTests.Infrastructure;

/// <summary>
/// Module discovery and the cross-module rule, as pure functions over <see cref="Layer"/>s so the rules can also be
/// proven on the fixture assembly (see ModuleRulesSanityTests).
/// </summary>
public static class ModuleRules
{
    public const string PublicApiSegment = "PublicApi";

    /// <summary>The module a type belongs to in this layer, or null when it is outside every module namespace.</summary>
    public static string? ModuleOf(Type type, Layer layer)
    {
        var ns = type.Namespace;
        var prefix = layer.ModuleRoot + ".";
        if (ns is null || !ns.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        return ns[prefix.Length..].Split('.')[0];
    }

    public static IReadOnlySet<string> DiscoverModules(Layer layer) =>
        SourceTypes(layer.Assembly)
            .Select(type => ModuleOf(type, layer))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

    public static IReadOnlySet<string> DiscoverModules(IEnumerable<Layer> layers) =>
        layers.SelectMany(DiscoverModules).ToHashSet(StringComparer.Ordinal);

    /// <summary>Top-level types written by hand: no nested types, nothing compiler- or tool-generated.</summary>
    public static IEnumerable<Type> SourceTypes(Assembly assembly) =>
        assembly.GetTypes().Where(static type => !type.IsNested && !IsGenerated(type));

    public static string PublicApiNamespace(Layer publicApiLayer, string module) =>
        $"{publicApiLayer.ModuleRoot}.{module}.{PublicApiSegment}";

    /// <summary>
    /// For every pair of modules X != Y and every layer, the types of X that depend on a type of Y, other than the
    /// types in Y's public API namespace (in <paramref name="publicApiLayer"/>). Returns "X type -> Y" entries.
    /// </summary>
    public static IReadOnlyList<string> FindCrossModuleViolations(IReadOnlyList<Layer> layers, Layer publicApiLayer)
    {
        var modules = DiscoverModules(layers);
        var violations = new List<string>();

        foreach (var target in modules)
        {
            var forbidden = ForbiddenTypeNames(layers, publicApiLayer, target);
            if (forbidden.Length == 0)
            {
                continue;
            }

            foreach (var source in modules.Where(module => module != target))
            {
                foreach (var layer in layers)
                {
                    var result = Types.InAssembly(layer.Assembly)
                        .That().ResideInNamespaceMatching(ModuleNamespacePattern(layer, source))
                        .ShouldNot().HaveDependencyOnAny(forbidden)
                        .GetResult();

                    violations.AddRange(FailingTypeNames(result).Select(name => $"{name} -> {target}"));
                }
            }
        }

        return violations;
    }

    /// <summary>Top-level types in the layer that are outside every module namespace and not an allowed root.</summary>
    public static IReadOnlyList<string> FindTypesOutsideModules(Layer layer, IReadOnlySet<string> allowed) =>
        SourceTypes(layer.Assembly)
            .Where(type => ModuleOf(type, layer) is null)
            .Select(static type => type.FullName ?? type.Name)
            .Where(name => !allowed.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Regex for a module's namespace and its sub-namespaces.</summary>
    public static string ModuleNamespacePattern(Layer layer, string module) =>
        $@"^{Regex.Escape(layer.ModuleRoot)}\.{Regex.Escape(module)}(\..+)?$";

    public static IReadOnlyList<string> FailingTypeNames(TestResult result) =>
        result.IsSuccessful ? [] : [.. result.FailingTypeNames ?? []];

    private static string[] ForbiddenTypeNames(IReadOnlyList<Layer> layers, Layer publicApiLayer, string module)
    {
        var publicApi = PublicApiNamespace(publicApiLayer, module);

        return layers
            .SelectMany(layer => SourceTypes(layer.Assembly)
                .Where(type => ModuleOf(type, layer) == module)
                .Where(type => !(layer == publicApiLayer && IsInNamespace(type, publicApi))))
            .Select(static type => type.FullName!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsInNamespace(Type type, string ns) =>
        type.Namespace == ns || (type.Namespace?.StartsWith(ns + ".", StringComparison.Ordinal) ?? false);

    private static bool IsGenerated(Type type) =>
        type.Name.StartsWith('<')
        || type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || type.IsDefined(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute), inherit: false);
}