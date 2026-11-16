namespace TherapyHub.Domain.Patients;

public readonly record struct PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}