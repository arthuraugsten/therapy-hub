using TherapyHub.Domain.Common;

namespace TherapyHub.Domain.Tests.Common;

public sealed class ValueObjectTests
{
    // 529.982.247-25 is a widely used *test* CPF with valid check digits. It belongs to no real person.
    private const string ValidFormattedCpf = "529.982.247-25";
    private const string ValidCpfDigits = "52998224725";
    private const string ValidCpfWrongSecondCheckDigit = "52998224726";
    private const string ValidCpfWrongFirstCheckDigit = "52998224715";
    private const string NormalizedEmail = "fake.patient@example.com";
    private const string EmailDomain = "@example.com";
    private const string Padding = "  ";
    private const char LocalPartChar = 'a';
    private const char ArabicIndicZero = '٠';
    private const char FullWidthZero = '０';

    [Theory]
    [InlineData(ValidFormattedCpf)]
    [InlineData(ValidCpfDigits)]
    public void CpfCreate_ValidCpf_StoresDigitsOnly(string value)
    {
        var result = Cpf.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidCpfDigits, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("529.982.247-26")] // wrong check digit
    [InlineData("111.111.111-11")] // repeated digits
    [InlineData("1234567890")]     // too short
    public void CpfCreate_InvalidCpf_ReturnsValidationError(string? value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Fact]
    public void EmailCreate_ValidEmail_NormalizesToLowercase()
    {
        var result = Email.Create("  Fake.Patient@Example.COM ");

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizedEmail, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("Fake Patient <fake@example.com>")]
    public void EmailCreate_InvalidEmail_ReturnsValidationError(string? value)
    {
        var result = Email.Create(value);

        Assert.Equal(EmailErrors.Invalid, result.Error);
    }

    [Fact]
    public void CpfCreate_NormalizedValidDigits_ValueEqualsInput()
    {
        var result = Cpf.Create(ValidCpfDigits);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidCpfDigits, result.Value.Value);
        Assert.Equal(Cpf.Length, result.Value.Value.Length);
    }

    [Fact]
    public void CpfCreate_FormattedAndNormalizedSameCpf_HaveSameValue()
    {
        var formatted = Cpf.Create(ValidFormattedCpf);
        var normalized = Cpf.Create(ValidCpfDigits);

        Assert.Equal(normalized.Value, formatted.Value);
    }

    [Theory]
    [InlineData("529 982 247 25")]
    [InlineData("cpf: 529a982b247c25")]
    [InlineData("  52998224725  ")]
    [InlineData("529.982.247/25")]
    public void CpfCreate_DigitsMixedWithLettersOrSpaces_IgnoresNonDigits(string value)
    {
        var result = Cpf.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidCpfDigits, result.Value.Value);
    }

    [Theory]
    [InlineData("529982247250")]           // 12 digits, no punctuation
    [InlineData("052998224725")]           // extra leading digit
    [InlineData("529.982.247-250")]        // 12 digits interleaved with punctuation
    [InlineData("0.529.982.247-25")]       // extra digit first, then a valid formatted CPF
    [InlineData("529.982.247-25 1")]       // extra digit after a valid formatted CPF
    [InlineData("5299822472552998224725")] // two valid CPFs concatenated
    public void CpfCreate_MoreThanElevenDigits_ReturnsValidationError(string value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData("5299822472")]  // 10 digits
    [InlineData("529982247-2")] // 11 characters, only 10 digits
    [InlineData("529.982.247-2")]
    [InlineData("5")]
    [InlineData("...........")] // 11 characters, no digits
    public void CpfCreate_FewerThanElevenDigits_ReturnsValidationError(string value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    [InlineData("000.000.000-00")]
    public void CpfCreate_AllSameDigits_ReturnsValidationError(string value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData(ValidCpfWrongSecondCheckDigit)] // fast path
    [InlineData(ValidCpfWrongFirstCheckDigit)]  // fast path
    [InlineData("529.982.247-15")]              // slow path, wrong first check digit
    [InlineData("529 982 247 26")]              // slow path, wrong second check digit
    public void CpfCreate_WrongCheckDigit_ReturnsValidationError(string value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData(ArabicIndicZero)]
    [InlineData(FullWidthZero)]
    public void CpfCreate_OnlyNonAsciiDigits_ReturnsValidationError(char zero)
    {
        var value = ToDigitsStartingAt(ValidCpfDigits, zero);

        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData(ArabicIndicZero)]
    [InlineData(FullWidthZero)]
    public void CpfCreate_NonAsciiDigitsMixedWithValidCpf_IgnoresNonAsciiDigits(char zero)
    {
        var value = ToDigitsStartingAt(ValidCpfDigits, zero) + ValidCpfDigits;

        var result = Cpf.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidCpfDigits, result.Value.Value);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("           ")] // 11 spaces
    public void CpfCreate_WhitespaceOnly_ReturnsValidationError(string value)
    {
        var result = Cpf.Create(value);

        Assert.Equal(CpfErrors.Invalid, result.Error);
    }

    [Fact]
    public void EmailCreate_AlreadyNormalized_ValueEqualsInput()
    {
        var result = Email.Create(NormalizedEmail);

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizedEmail, result.Value.Value);
    }

    [Theory]
    [InlineData("  fake.patient@example.com  ")]
    [InlineData("\tfake.patient@example.com\n")]
    [InlineData("fake.patient@example.com ")]
    public void EmailCreate_NeedsOnlyTrimming_ReturnsTrimmedValue(string value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizedEmail, result.Value.Value);
    }

    [Theory]
    [InlineData("Fake.Patient@Example.com")]
    [InlineData("FAKE.PATIENT@EXAMPLE.COM")]
    [InlineData("fake.patient@example.coM")]
    public void EmailCreate_NeedsOnlyLowercasing_ReturnsLowercasedValue(string value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizedEmail, result.Value.Value);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void EmailCreate_WhitespaceOnly_ReturnsValidationError(string value)
    {
        var result = Email.Create(value);

        Assert.Equal(EmailErrors.Invalid, result.Error);
    }

    [Fact]
    public void EmailCreate_LengthExactlyMaxLength_ReturnsSuccess()
    {
        var value = EmailOfLength(Email.MaxLength);

        var result = Email.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void EmailCreate_MaxLengthWithPaddingAndUppercase_ReturnsNormalizedValue()
    {
        var value = EmailOfLength(Email.MaxLength);

        var result = Email.Create(Padding + value.ToUpperInvariant() + Padding);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void EmailCreate_MaxLengthWithPadding_ReturnsTrimmedValue()
    {
        var value = EmailOfLength(Email.MaxLength);

        var result = Email.Create(Padding + value + Padding);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmailCreate_LengthAboveMaxLength_ReturnsValidationError(bool padded)
    {
        var email = EmailOfLength(Email.MaxLength + 1);
        var value = padded ? Padding + email + Padding : email;

        var result = Email.Create(value);

        Assert.Equal(EmailErrors.Invalid, result.Error);
    }

    [Theory]
    [InlineData("<fake.patient@example.com>")]
    [InlineData("\"Fake Patient\" <fake.patient@example.com>")]
    [InlineData("  Fake Patient <fake.patient@example.com>  ")]
    public void EmailCreate_DisplayNameForm_ReturnsValidationError(string value)
    {
        var result = Email.Create(value);

        Assert.Equal(EmailErrors.Invalid, result.Error);
    }

    private static string ToDigitsStartingAt(string asciiDigits, char zero) =>
        string.Concat(asciiDigits.Select(c => (char)(zero + (c - '0'))));

    private static string EmailOfLength(int length) =>
        new string(LocalPartChar, length - EmailDomain.Length) + EmailDomain;
}