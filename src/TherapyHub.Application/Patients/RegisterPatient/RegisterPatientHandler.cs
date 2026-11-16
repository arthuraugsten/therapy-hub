using TherapyHub.Application.Abstractions.Authentication;
using TherapyHub.Application.Abstractions.Data;
using TherapyHub.Application.Abstractions.Messaging;
using TherapyHub.Domain.Abstractions;
using TherapyHub.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients.RegisterPatient;

internal sealed class RegisterPatientHandler(
    IPatientRepository patients,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<RegisterPatientCommand, PatientDto>
{
    public async Task<Result<PatientDto>> HandleAsync(RegisterPatientCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        Cpf? cpf = null;
        if (!string.IsNullOrWhiteSpace(command.Cpf))
        {
            var cpfResult = Cpf.Create(command.Cpf);
            if (cpfResult.IsFailure)
            {
                return cpfResult.Error;
            }

            cpf = cpfResult.Value;
        }

        var psychologistId = currentUser.PsychologistId;

        if (await patients.IsEmailInUseAsync(psychologistId, email.Value, cancellationToken))
        {
            return PatientErrors.EmailAlreadyInUse;
        }

        var patient = Patient.Register(
            psychologistId,
            command.FullName,
            email.Value,
            cpf,
            command.BirthDate,
            timeProvider.GetUtcNow());

        if (patient.IsFailure)
        {
            return patient.Error;
        }

        patients.Add(patient.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientDto.From(patient.Value);
    }
}