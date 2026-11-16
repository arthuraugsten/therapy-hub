using NSubstitute;

using TherapyHub.Application.Patients;
using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Tests.Patients;

public sealed class PatientsApiTests
{
    private static readonly PsychologistId CurrentPsychologistId = new(Guid.NewGuid());

    private readonly IPatientQueries _queries = Substitute.For<IPatientQueries>();
    private readonly PatientsApi _api;

    public PatientsApiTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.PsychologistId.Returns(CurrentPsychologistId);

        _api = new PatientsApi(_queries, currentUser);
    }

    [Fact]
    public async Task ExistsAsync_PatientOfCurrentPsychologist_ReturnsTrue()
    {
        var patientId = Guid.NewGuid();
        _queries.ExistsAsync(new PatientId(patientId), CurrentPsychologistId, Arg.Any<CancellationToken>()).Returns(true);

        var exists = await _api.ExistsAsync(patientId, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_PatientNotFound_ReturnsFalse()
    {
        _queries.ExistsAsync(Arg.Any<PatientId>(), Arg.Any<PsychologistId>(), Arg.Any<CancellationToken>()).Returns(false);

        var exists = await _api.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_PatientOfOtherPsychologist_ReturnsFalse()
    {
        var patientId = Guid.NewGuid();
        var otherPsychologistId = new PsychologistId(Guid.NewGuid());
        _queries.ExistsAsync(new PatientId(patientId), otherPsychologistId, Arg.Any<CancellationToken>()).Returns(true);

        var exists = await _api.ExistsAsync(patientId, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_AnyPatient_QueriesWithCurrentPsychologistIdAndToken()
    {
        var patientId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();

        await _api.ExistsAsync(patientId, cancellation.Token);

        await _queries.Received(1).ExistsAsync(new PatientId(patientId), CurrentPsychologistId, cancellation.Token);
    }
}