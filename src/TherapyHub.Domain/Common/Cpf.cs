using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Common;

/// <summary>Brazilian individual taxpayer ID (CPF). Stored as 11 digits, without punctuation.</summary>
public sealed record Cpf
{
    public const int Length = 11;
    public const int FormattedLength = 14; // "000.000.000-00"

    private Cpf(string value) => Value = value;

    public string Value { get; }

    public static Result<Cpf> Create(string? value)
    {
        // Fast path: input is already exactly 11 ASCII digits, so it can be stored as is.
        if (value is not null && value.Length == Length && !value.AsSpan().ContainsAnyExceptInRange('0', '9'))
        {
            return IsValid(value) ? new Cpf(value) : CpfErrors.Invalid;
        }

        Span<char> digits = stackalloc char[Length];
        var count = 0;
        foreach (var c in value.AsSpan())
        {
            if (!char.IsAsciiDigit(c))
            {
                continue;
            }

            if (count == Length)
            {
                return CpfErrors.Invalid;
            }

            digits[count++] = c;
        }

        return count != Length || !IsValid(digits)
            ? (Result<Cpf>)CpfErrors.Invalid
            : (Result<Cpf>)new Cpf(new string(digits));
    }

    public override string ToString() => Value;

    private static bool IsValid(ReadOnlySpan<char> digits) =>
        digits.IndexOfAnyExcept(digits[0]) >= 0 && HasValidCheckDigits(digits);

    // Receita Federal algorithm: digits 10 and 11 are check digits (weighted sum modulo 11).
    private static bool HasValidCheckDigits(ReadOnlySpan<char> digits) =>
        CheckDigit(digits, 9) == digits[9] - '0' &&
        CheckDigit(digits, 10) == digits[10] - '0';

    private static int CheckDigit(ReadOnlySpan<char> digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}