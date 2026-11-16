using TherapyHub.Application.Patients;

namespace TherapyHub.Api.Features.Patients.RegisterPatient;

public sealed record RegisterPatientResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Cpf,
    DateOnly? BirthDate,
    DateTimeOffset RegisteredAt)
{
    public static RegisterPatientResponse From(PatientDto patient) => new(
        patient.Id,
        patient.FullName,
        patient.Email,
        patient.Cpf,
        patient.BirthDate,
        patient.RegisteredAt);
}