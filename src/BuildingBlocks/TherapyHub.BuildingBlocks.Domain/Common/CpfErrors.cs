using TherapyHub.BuildingBlocks.Domain.Abstractions;

namespace TherapyHub.BuildingBlocks.Domain.Common;

public static class CpfErrors
{
    public static readonly Error Invalid =
        Error.Validation("Cpf.Invalid", "The CPF is not valid.");
}