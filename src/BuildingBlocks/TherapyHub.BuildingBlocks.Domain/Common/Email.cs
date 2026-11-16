using System.Net.Mail;

using TherapyHub.BuildingBlocks.Domain.Abstractions;

namespace TherapyHub.BuildingBlocks.Domain.Common;

public sealed record Email
{
    /// <summary>Maximum length of an email address (RFC 5321 path limit).</summary>
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        var trimmed = value.AsSpan().Trim();
        if (value is null || trimmed.IsEmpty || trimmed.Length > MaxLength)
        {
            return EmailErrors.Invalid;
        }

        Span<char> buffer = stackalloc char[MaxLength];
        var lowered = buffer[..trimmed.ToLowerInvariant(buffer)];

        // Reuse the input when trimming and lowering changed nothing; otherwise allocate once.
        var normalized = lowered.SequenceEqual(value) ? value : new string(lowered);

        return !MailAddress.TryCreate(normalized, out var address) || address.Address != normalized
            ? (Result<Email>)EmailErrors.Invalid
            : (Result<Email>)new Email(normalized);
    }

    public override string ToString() => Value;
}