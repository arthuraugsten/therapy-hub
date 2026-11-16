using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Common;

public static class CpfErrors
{
    public static readonly Error Invalid =
        Error.Validation("Cpf.Invalid", "The CPF is not valid.");
}