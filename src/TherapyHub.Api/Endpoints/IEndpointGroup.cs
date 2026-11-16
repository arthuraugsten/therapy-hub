namespace TherapyHub.Api.Endpoints;

/// <summary>
/// A feature's route group: owns the prefix and everything its endpoints share (tags, authorization, filters).
/// One per feature, e.g. <c>Features/Patients/PatientsEndpointGroup.cs</c>.
/// </summary>
public interface IEndpointGroup
{
    RouteGroupBuilder MapGroup(IEndpointRouteBuilder app);
}