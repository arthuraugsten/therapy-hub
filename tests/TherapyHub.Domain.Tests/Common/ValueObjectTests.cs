using TherapyHub.Domain.Common;

namespace TherapyHub.Domain.Tests.Common;

public sealed class ValueObjectTests
{
    // 529.982.247-25 is a widely used *test* CPF with valid check digits. It belongs to no real person.
    private const string ValidFormattedCpf = "529.982.247-25";
    private const string ValidCpfDigits = "52998224725";
    private const string NormalizedEmail = "fake.patient@example.com";

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
}