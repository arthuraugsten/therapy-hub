using Microsoft.AspNetCore.Routing;

namespace TherapyHub.BuildingBlocks.Presentation.Endpoints;

/// <summary>
/// One REPR endpoint. Don't implement this directly: implement <see cref="IEndpoint{TGroup}"/> so the endpoint
/// is mapped inside its feature's group.
/// </summary>
public interface IEndpoint
{
    /// <summary>Maps the endpoint on its group. Routes are relative to the group prefix.</summary>
    void MapEndpoint(RouteGroupBuilder group);
}

/// <summary>An endpoint that belongs to <typeparamref name="TGroup"/>. Discovered and mapped by <see cref="EndpointExtensions"/>.</summary>
public interface IEndpoint<TGroup> : IEndpoint
    where TGroup : IEndpointGroup;