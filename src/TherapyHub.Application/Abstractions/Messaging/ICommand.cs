using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Application.Abstractions.Messaging;

/// <summary>A request that changes state and returns <typeparamref name="TResult"/> on success.</summary>
public interface ICommand<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}