using System.Net;
using System.Net.Http.Json;

using TherapyHub.Api.Features.Patients.GetPatientById;
using TherapyHub.Api.Features.Patients.RegisterPatient;
using TherapyHub.Api.IntegrationTests.Infrastructure;

namespace TherapyHub.Api.IntegrationTests.Patients;

public sealed class PatientEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string PatientsPath = "/patients";
    private const string FullName = "Fake Patient";

    // Widely used *test* CPF with valid check digits; belongs to no real person.
    private const string ValidFormattedCpf = "529.982.247-25";
    private const string ValidCpfDigits = "52998224725";

    private static readonly DateOnly BirthDate = new(1990, 1, 1);

    [Fact]
    public async Task RegisterPatient_ValidRequest_Returns201AndPatientCanBeRead()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());
        var request = new RegisterPatientRequest(FullName, UniqueEmail(), ValidFormattedCpf, BirthDate);

        var response = await client.PostAsJsonAsync(PatientsPath, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RegisterPatientResponse>();
        Assert.NotNull(created);
        Assert.Equal($"{PatientsPath}/{created.Id}", response.Headers.Location?.ToString());

        var patient = await client.GetFromJsonAsync<GetPatientByIdResponse>(response.Headers.Location);
        Assert.Equal(created.Id, patient?.Id);
        Assert.Equal(ValidCpfDigits, patient?.Cpf);
    }

    [Fact]
    public async Task RegisterPatient_DuplicateEmail_Returns409()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());
        var request = new RegisterPatientRequest(FullName, UniqueEmail(), null, null);
        await client.PostAsJsonAsync(PatientsPath, request);

        var response = await client.PostAsJsonAsync(PatientsPath, request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPatient_InvalidRequest_Returns400()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());
        var request = new RegisterPatientRequest(string.Empty, "not-an-email", null, null);

        var response = await client.PostAsJsonAsync(PatientsPath, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPatient_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var request = new RegisterPatientRequest(FullName, UniqueEmail(), null, null);

        var response = await client.PostAsJsonAsync(PatientsPath, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientById_OtherPsychologistsPatient_Returns404()
    {
        var owner = factory.CreateClientFor(Guid.NewGuid());
        var created = await owner.PostAsJsonAsync(
            PatientsPath,
            new RegisterPatientRequest(FullName, UniqueEmail(), null, null));

        var intruder = factory.CreateClientFor(Guid.NewGuid());
        var response = await intruder.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string UniqueEmail() => $"fake.{Guid.NewGuid():N}@example.com";
}