using TherapyHub.BuildingBlocks.Presentation.Endpoints;

namespace TherapyHub.Api.Features.Patients;

/// <summary>All patient endpoints: under /patients, tagged "Patients", authenticated psychologists only.</summary>
internal sealed class PatientsEndpointGroup : IEndpointGroup
{
    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app) =>
        app.MapGroup("/patients")
            .WithTags("Patients")
            .RequireAuthorization();
}