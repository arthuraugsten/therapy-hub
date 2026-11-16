using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Patients;

public static class PatientErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Patient.NameRequired", "The patient's full name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Patient.NameTooLong", $"The patient's full name must have at most {Patient.FullNameMaxLength} characters.");

    public static readonly Error BirthDateInFuture =
        Error.Validation("Patient.BirthDateInFuture", "The birth date cannot be in the future.");

    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("Patient.EmailAlreadyInUse", "Another patient already uses this email address.");

    public static readonly Error AlreadyArchived =
        Error.Conflict("Patient.AlreadyArchived", "The patient is already archived.");

    public static readonly Error NotFound =
        Error.NotFound("Patient.NotFound", "The patient was not found.");
}