using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Common;

public static class EmailErrors
{
    public static readonly Error Invalid =
        Error.Validation("Email.Invalid", "The email address is not valid.");
}