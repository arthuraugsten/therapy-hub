using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Patients.Events;

namespace TherapyHub.Domain.Tests.Patients;

public sealed class PatientTests
{
    private const string FullName = "Fake Patient";

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddDays(1);
    private static readonly DateOnly BirthDate = new(1990, 1, 1);
    private static readonly PsychologistId PsychologistId = new(Guid.NewGuid());
    private static readonly Email Email = Email.Create("fake.patient@example.com").Value;

    [Fact]
    public void Register_ValidData_CreatesPatientAndRaisesEvent()
    {
        var result = Patient.Register(PsychologistId, $"  {FullName}  ", Email, null, BirthDate, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(FullName, result.Value.FullName);
        Assert.Equal(PsychologistId, result.Value.PsychologistId);
        Assert.False(result.Value.IsArchived);

        var domainEvent = Assert.IsType<PatientRegistered>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.PatientId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_EmptyName_ReturnsNameRequired(string fullName)
    {
        var result = Patient.Register(PsychologistId, fullName, Email, null, null, Now);

        Assert.Equal(PatientErrors.NameRequired, result.Error);
    }

    [Fact]
    public void Register_NameTooLong_ReturnsNameTooLong()
    {
        var result = Patient.Register(PsychologistId, new string('a', Patient.FullNameMaxLength + 1), Email, null, null, Now);

        Assert.Equal(PatientErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void Register_BirthDateInFuture_ReturnsBirthDateInFuture()
    {
        var tomorrow = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(1);

        var result = Patient.Register(PsychologistId, FullName, Email, null, tomorrow, Now);

        Assert.Equal(PatientErrors.BirthDateInFuture, result.Error);
    }

    [Fact]
    public void Archive_ActivePatient_SetsArchivedAt()
    {
        var patient = Patient.Register(PsychologistId, FullName, Email, null, null, Now).Value;

        var result = patient.Archive(Later);

        Assert.True(result.IsSuccess);
        Assert.True(patient.IsArchived);
        Assert.Equal(Later, patient.ArchivedAt);
    }

    [Fact]
    public void Archive_AlreadyArchived_ReturnsAlreadyArchived()
    {
        var patient = Patient.Register(PsychologistId, FullName, Email, null, null, Now).Value;
        patient.Archive(Now);

        var result = patient.Archive(Now);

        Assert.Equal(PatientErrors.AlreadyArchived, result.Error);
    }
}