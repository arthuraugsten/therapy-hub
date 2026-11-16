using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Application.Abstractions.Authentication;

/// <summary>The authenticated psychologist. Every query and command must be scoped to this ID.</summary>
public interface ICurrentUser
{
    PsychologistId PsychologistId { get; }
}