using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

using TherapyHub.Api.Features.Patients.RegisterPatient;
using TherapyHub.Api.IntegrationTests.Infrastructure;
using TherapyHub.Application.Patients;
using TherapyHub.Application.Patients.PublicApi;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Api.IntegrationTests.Patients;

/// <summary>
/// Resolves the Patients module's public API from the real container against the real database. The current user is
/// the production <c>CurrentUser</c>, fed by an <see cref="HttpContext"/> set on the accessor for the call.
/// </summary>
public sealed class PatientsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string PatientsPath = "/patients";
    private const string FullName = "Fake Patient";

    private static readonly DateTimeOffset ArchivedAt = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExistsAsync_OwnPatient_ReturnsTrue()
    {
        var psychologistId = Guid.NewGuid();
        var patientId = await RegisterPatientAsync(psychologistId);

        var exists = await ExistsAsAsync(psychologistId, patientId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_OtherPsychologistsPatient_ReturnsFalse()
    {
        var patientId = await RegisterPatientAsync(Guid.NewGuid());

        var exists = await ExistsAsAsync(Guid.NewGuid(), patientId);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_UnknownPatient_ReturnsFalse()
    {
        var exists = await ExistsAsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_ArchivedOwnPatient_ReturnsFalse()
    {
        var psychologistId = Guid.NewGuid();
        var patientId = await RegisterPatientAsync(psychologistId);
        await ArchiveAsync(psychologistId, patientId);

        var exists = await ExistsAsAsync(psychologistId, patientId);

        Assert.False(exists);
    }

    private async Task<Guid> RegisterPatientAsync(Guid psychologistId)
    {
        var client = factory.CreateClientFor(psychologistId);
        var request = new RegisterPatientRequest(FullName, $"fake.{Guid.NewGuid():N}@example.com", null, null);

        var response = await client.PostAsJsonAsync(PatientsPath, request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<RegisterPatientResponse>();
        return created!.Id;
    }

    // No archive endpoint exists yet: archive through the module's own ports and the domain behavior.
    private async Task ArchiveAsync(Guid psychologistId, Guid patientId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IPatientsUnitOfWork>();

        var patient = await repository.GetByIdAsync(
            new PatientId(patientId),
            new PsychologistId(psychologistId),
            CancellationToken.None);
        Assert.NotNull(patient);
        Assert.True(patient.Archive(ArchivedAt).IsSuccess);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<bool> ExistsAsAsync(Guid psychologistId, Guid patientId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, psychologistId.ToString())],
            TestAuthHandler.SchemeName);
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = scope.ServiceProvider,
        };

        try
        {
            var patientsApi = scope.ServiceProvider.GetRequiredService<IPatientsApi>();
            return await patientsApi.ExistsAsync(patientId, CancellationToken.None);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }
}