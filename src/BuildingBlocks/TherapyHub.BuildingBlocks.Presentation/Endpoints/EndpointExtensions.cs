using System.Reflection;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TherapyHub.BuildingBlocks.Presentation.Endpoints;

public static class EndpointExtensions
{
    /// <summary>Registers every <see cref="IEndpointGroup"/> and every <see cref="IEndpoint{TGroup}"/> in the assembly.</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var concreteTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .ToList();

        services.TryAddEnumerable(concreteTypes
            .Where(type => type.IsAssignableTo(typeof(IEndpointGroup)))
            .Select(type => ServiceDescriptor.Singleton(typeof(IEndpointGroup), type)));

        foreach (var endpointType in concreteTypes.Where(type => type.IsAssignableTo(typeof(IEndpoint))))
        {
            var groupedContract = endpointType.GetInterfaces()
                .SingleOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEndpoint<>))
                ?? throw new InvalidOperationException(
                    $"{endpointType.Name} must implement IEndpoint<TGroup> so it is mapped inside an endpoint group.");

            services.TryAddEnumerable(ServiceDescriptor.Transient(groupedContract, endpointType));
        }

        return services;
    }

    /// <summary>Maps each group once, then maps the group's endpoints on it.</summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        foreach (var group in app.ServiceProvider.GetServices<IEndpointGroup>())
        {
            var groupBuilder = group.MapGroup(app);
            var endpointContract = typeof(IEndpoint<>).MakeGenericType(group.GetType());

            foreach (var endpoint in app.ServiceProvider.GetServices(endpointContract).Cast<IEndpoint>())
                endpoint.MapEndpoint(groupBuilder);
        }

        return app;
    }
}