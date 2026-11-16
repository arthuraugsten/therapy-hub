using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using TherapyHub.Application.Patients;
using TherapyHub.Application.Patients.RegisterPatient;
using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Tests.Patients;

public sealed class RegisterPatientHandlerTests
{
    private const string FullName = "Fake Patient";
    private const string EmailAddress = "fake.patient@example.com";

    // Widely used *test* CPF with valid check digits; belongs to no real person.
    private const string ValidFormattedCpf = "529.982.247-25";
    private const string ValidCpfDigits = "52998224725";
    private const string InvalidCpf = "123.456.789-00";

    private static readonly PsychologistId PsychologistId = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IPatientsUnitOfWork _unitOfWork = Substitute.For<IPatientsUnitOfWork>();
    private readonly RegisterPatientHandler _handler;

    public RegisterPatientHandlerTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.PsychologistId.Returns(PsychologistId);

        _handler = new RegisterPatientHandler(_patients, _unitOfWork, currentUser, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsPatientForCurrentPsychologistAndSaves()
    {
        var command = new RegisterPatientCommand(FullName, EmailAddress, ValidFormattedCpf, null);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidCpfDigits, result.Value.Cpf);
        Assert.Equal(Now, result.Value.RegisteredAt);
        _patients.Received(1).Add(Arg.Is<Patient>(p => p.PsychologistId == PsychologistId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailInUse_ReturnsConflictAndDoesNotSave()
    {
        _patients.IsEmailInUseAsync(PsychologistId, Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterPatientCommand(FullName, EmailAddress, null, null);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(PatientErrors.EmailAlreadyInUse, result.Error);
        _patients.DidNotReceive().Add(Arg.Any<Patient>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidCpf_ReturnsValidationError()
    {
        var command = new RegisterPatientCommand(FullName, EmailAddress, InvalidCpf, null);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CpfErrors.Invalid, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}