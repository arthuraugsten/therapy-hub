using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TherapyHub.Core.Application;
using TherapyHub.Patients.Application;
using TherapyHub.Patients.Application.Repositories;
using TherapyHub.Patients.Infrastructure;
using TherapyHub.Patients.Infrastructure.Repositories;
using TherapyHub.Patients.Presentation.Endpoints.CreatePatient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(ApplicationAssemblyInfo.Assembly);
    config.AddBehavior(typeof(PipelineRequestValidator<,>));
});

builder.Services.AddValidatorsFromAssembly(ApplicationAssemblyInfo.Assembly);

builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PatientsDb"),
        optionsBuilder =>
        {
            optionsBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(20), []);
            optionsBuilder.MigrationsHistoryTable(PatientDbContext.MigrationHistoryTableName, PatientDbContext.SchemaName);
        }
    )
);

builder.Services.AddScoped<IPatientRepository, PatientRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPatientEndpoint();

app.Run();

