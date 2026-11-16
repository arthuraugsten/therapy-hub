using TherapyHub.Domain.Abstractions;

namespace TherapyHub.Domain.Common;

/// <summary>Brazilian individual taxpayer ID (CPF). Stored as 11 digits, without punctuation.</summary>
public sealed record Cpf
{
    public const int Length = 11;
    public const int FormattedLength = 14; // "000.000.000-00"

    private Cpf(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Cpf> Create(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        if (digits.Length != Length || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
        {
            return CpfErrors.Invalid;
        }

        return new Cpf(digits);
    }

    public override string ToString() => Value;

    // Receita Federal algorithm: digits 10 and 11 are check digits (weighted sum modulo 11).
    private static bool HasValidCheckDigits(string digits) =>
        CheckDigit(digits, 9) == digits[9] - '0' &&
        CheckDigit(digits, 10) == digits[10] - '0';

    private static int CheckDigit(string digits, int length)
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