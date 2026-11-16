using FluentValidation;

using TherapyHub.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Api.Features.Patients.RegisterPatient;

/// <summary>Checks the request shape only. Business rules (CPF check digits, duplicates) live in Domain/Application.</summary>
internal sealed class RegisterPatientValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientValidator()
    {
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(Patient.FullNameMaxLength);
        RuleFor(r => r.Email).NotEmpty().MaximumLength(Email.MaxLength).EmailAddress();
        RuleFor(r => r.Cpf).MaximumLength(Cpf.FormattedLength).When(r => r.Cpf is not null); // "000.000.000-00"
    }
}