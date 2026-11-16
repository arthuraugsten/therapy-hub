namespace TherapyHub.Application.Patients.PublicApi;

/// <summary>
/// The Patients module's contract for other modules. This namespace holds only interfaces and sealed records with
/// primitive types: no domain types, no strongly-typed IDs. Every call is scoped to the current psychologist.
/// </summary>
public interface IPatientsApi
{
    /// <summary>
    /// True when the patient exists, belongs to the current psychologist, and is not archived.
    /// Another psychologist's patient is reported as false.
    /// </summary>
    Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken);
}