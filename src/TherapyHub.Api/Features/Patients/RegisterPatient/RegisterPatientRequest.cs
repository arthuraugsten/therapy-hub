using TherapyHub.Application.Patients.RegisterPatient;

namespace TherapyHub.Api.Features.Patients.RegisterPatient;

public sealed record RegisterPatientRequest(
    string FullName,
    string Email,
    string? Cpf,
    DateOnly? BirthDate)
{
    public RegisterPatientCommand ToCommand() => new(FullName, Email, Cpf, BirthDate);
}