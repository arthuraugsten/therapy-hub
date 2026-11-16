using TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;

namespace TherapyHub.Application.Patients.RegisterPatient;

public sealed record RegisterPatientCommand(
    string FullName,
    string Email,
    string? Cpf,
    DateOnly? BirthDate) : ICommand<PatientDto>;