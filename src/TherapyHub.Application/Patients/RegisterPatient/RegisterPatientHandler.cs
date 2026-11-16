using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;
using TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;
using TherapyHub.BuildingBlocks.Domain.Abstractions;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients.RegisterPatient;

internal sealed class RegisterPatientHandler : ICommandHandler<RegisterPatientCommand, PatientDto>
{
    private readonly IPatientRepository _patients;
    private readonly IPatientsUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RegisterPatientHandler(
        IPatientRepository patients,
        IPatientsUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _patients = patients;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

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

        var psychologistId = _currentUser.PsychologistId;

        if (await _patients.IsEmailInUseAsync(psychologistId, email.Value, cancellationToken))
        {
            return PatientErrors.EmailAlreadyInUse;
        }

        var patient = Patient.Register(
            psychologistId,
            command.FullName,
            email.Value,
            cpf,
            command.BirthDate,
            _timeProvider.GetUtcNow());

        if (patient.IsFailure)
        {
            return patient.Error;
        }

        _patients.Add(patient.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientDto.From(patient.Value);
    }
}