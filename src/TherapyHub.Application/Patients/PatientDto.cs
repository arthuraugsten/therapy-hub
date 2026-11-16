using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients;

public sealed record PatientDto(
    Guid Id,
    string FullName,
    string Email,
    string? Cpf,
    DateOnly? BirthDate,
    DateTimeOffset RegisteredAt)
{
    public static PatientDto From(Patient patient) => new(
        patient.Id.Value,
        patient.FullName,
        patient.Email.Value,
        patient.Cpf?.Value,
        patient.BirthDate,
        patient.RegisteredAt);
}