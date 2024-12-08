using MediatR;
using TherapyHub.Core.Application;
using TherapyHub.Patients.Application.Repositories;
using TherapyHub.Patients.Domain;

namespace TherapyHub.Patients.Application.Commands.CreatePatient;

public sealed class CreatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<CreatePatientCommand, Result<CreatePatientCommandResult>>
{
    public async Task<Result<CreatePatientCommandResult>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = Patient.Create(request.Name);
        await patientRepository.AddAsync(patient, cancellationToken);
        await patientRepository.SaveAsync(cancellationToken);

        return Result<CreatePatientCommandResult>.Success(new(patient.Id));
    }
}
