using Microsoft.AspNetCore.Http.HttpResults;

using TherapyHub.Application.Patients;
using TherapyHub.Application.Patients.RegisterPatient;
using TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;
using TherapyHub.BuildingBlocks.Presentation.Endpoints;

namespace TherapyHub.Api.Features.Patients.RegisterPatient;

internal sealed class RegisterPatientEndpoint : IEndpoint<PatientsEndpointGroup>
{
    public void MapEndpoint(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("RegisterPatient")
            .WithRequestValidation<RegisterPatientRequest>();

    private static async Task<Results<Created<RegisterPatientResponse>, ProblemHttpResult>> HandleAsync(
        RegisterPatientRequest request,
        ICommandHandler<RegisterPatientCommand, PatientDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/patients/{result.Value.Id}", RegisterPatientResponse.From(result.Value))
            : result.Error.ToProblem();
    }
}