using TherapyHub.BuildingBlocks.Domain.Abstractions;

namespace TherapyHub.BuildingBlocks.Domain.Common;

public static class EmailErrors
{
    public static readonly Error Invalid =
        Error.Validation("Email.Invalid", "The email address is not valid.");
}