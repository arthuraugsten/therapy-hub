using Microsoft.Extensions.DependencyInjection;

using TherapyHub.Application.Abstractions.Messaging;

namespace TherapyHub.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>),
    ];

    /// <summary>Registers every command, query, and domain event handler in this assembly.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var registrations =
            from type in typeof(DependencyInjection).Assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition())
            select (contract, type);

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }

        return services;
    }
}