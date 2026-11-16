using System.Net.Mail;

using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Common;

public sealed record Email
{
    /// <summary>Maximum length of an email address (RFC 5321 path limit).</summary>
    public const int MaxLength = 254;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized) || normalized.Length > MaxLength ||
            !MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
        {
            return EmailErrors.Invalid;
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}