using FluentValidation;

namespace TherapyHub.Patients.Application.Commands.CreatePatient;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(t => t.Name)
            .NotEmpty()
            .WithErrorCode("PATIENT-001")
            .MinimumLength(3)
            .WithErrorCode("PATIENT-002")
            .MaximumLength(256)
            .WithErrorCode("PATIENT-003");
    }
}
