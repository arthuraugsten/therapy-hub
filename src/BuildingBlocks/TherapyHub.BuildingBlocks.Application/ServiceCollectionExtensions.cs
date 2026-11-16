using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;

namespace TherapyHub.BuildingBlocks.Application;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>),
    ];

    /// <summary>Registers every command, query, and domain event handler in <paramref name="assembly"/> as scoped.</summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
                continue;

            foreach (var contract in type.GetInterfaces())
            {
                if (contract.IsGenericType && Array.IndexOf(HandlerInterfaces, contract.GetGenericTypeDefinition()) >= 0)
                    services.AddScoped(contract, type);
            }
        }

        return services;
    }
}