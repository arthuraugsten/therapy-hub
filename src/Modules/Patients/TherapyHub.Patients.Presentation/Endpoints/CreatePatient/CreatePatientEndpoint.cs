using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TherapyHub.Patients.Application.Commands.CreatePatient;

namespace TherapyHub.Patients.Presentation.Endpoints.CreatePatient;

public static class CreatePatientEndpoint
{
    public static void MapPatientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/patient", async (IMediator mediator, CreatePatientRequest request, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreatePatientCommand(request.Name), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.CreatedAtRoute("/patient/{id}", new { id = result.Value!.Id });
            }

            return Results.BadRequest(result.Errors);
        });
    }
}
