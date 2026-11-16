using TherapyHub.BuildingBlocks.Domain.Abstractions;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients.Events;

namespace TherapyHub.Domain.Patients;

/// <summary>
/// A person under the care of one psychologist. Holds demographics and contact info only.
/// Clinical content lives in the ClinicalRecord aggregate, never here.
/// </summary>
public sealed class Patient : AggregateRoot<PatientId>
{
    public const int FullNameMaxLength = 200;

    private Patient(
        PatientId id,
        PsychologistId psychologistId,
        string fullName,
        Email email,
        Cpf? cpf,
        DateOnly? birthDate,
        DateTimeOffset registeredAt)
        : base(id)
    {
        PsychologistId = psychologistId;
        FullName = fullName;
        Email = email;
        Cpf = cpf;
        BirthDate = birthDate;
        RegisteredAt = registeredAt;
    }

    // Required by EF Core.
    private Patient()
        : base(default)
    {
        FullName = null!;
        Email = null!;
    }

    public PsychologistId PsychologistId { get; private set; }

    public string FullName { get; private set; }

    public Email Email { get; private set; }

    public Cpf? Cpf { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static Result<Patient> Register(
        PsychologistId psychologistId,
        string fullName,
        Email email,
        Cpf? cpf,
        DateOnly? birthDate,
        DateTimeOffset now)
    {
        var name = fullName?.Trim();

        if (string.IsNullOrEmpty(name))
            return PatientErrors.NameRequired;

        if (name.Length > FullNameMaxLength)
            return PatientErrors.NameTooLong;

        if (birthDate is { } date && date > DateOnly.FromDateTime(now.UtcDateTime))
            return PatientErrors.BirthDateInFuture;

        var patient = new Patient(PatientId.New(), psychologistId, name, email, cpf, birthDate, now);
        patient.Raise(new PatientRegistered(patient.Id, psychologistId, now));

        return patient;
    }

    /// <summary>Soft delete. The patient is hidden from queries but kept to satisfy record-retention rules.</summary>
    public Result Archive(DateTimeOffset now)
    {
        if (IsArchived)
            return Result.Failure(PatientErrors.AlreadyArchived);

        ArchivedAt = now;
        return Result.Success();
    }
}