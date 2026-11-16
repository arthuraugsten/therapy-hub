using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Application.Abstractions.Messaging;

/// <summary>A read-only request. Query handlers never change state.</summary>
public interface IQuery<TResult>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}