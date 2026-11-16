using Microsoft.AspNetCore.Http.HttpResults;

using TherapyHub.Application.Patients;
using TherapyHub.Application.Patients.GetPatientById;
using TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;
using TherapyHub.BuildingBlocks.Presentation.Endpoints;

namespace TherapyHub.Api.Features.Patients.GetPatientById;

internal sealed class GetPatientByIdEndpoint : IEndpoint<PatientsEndpointGroup>
{
    public void MapEndpoint(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetPatientById");

    private static async Task<Results<Ok<GetPatientByIdResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        IQueryHandler<GetPatientByIdQuery, PatientDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPatientByIdQuery(id), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(GetPatientByIdResponse.From(result.Value))
            : result.Error.ToProblem();
    }
}