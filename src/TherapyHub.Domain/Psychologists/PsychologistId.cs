namespace TherapyHub.Domain.Psychologists;

/// <summary>Identifies a psychologist. Equal to the subject ("sub") of the psychologist's Keycloak account.</summary>
public readonly record struct PsychologistId(Guid Value)
{
    public override string ToString() => Value.ToString();
}