using MediatR;
using TherapyHub.Core.Application;

namespace TherapyHub.Patients.Application.Commands.CreatePatient;

public sealed record CreatePatientCommand(string Name) : IRequest<Result<CreatePatientCommandResult>>;
