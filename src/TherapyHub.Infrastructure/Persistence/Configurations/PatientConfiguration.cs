using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TherapyHub.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable(nameof(TherapyHubDbContext.Patients));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PatientId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.PsychologistId)
            .HasConversion(id => id.Value, value => new PsychologistId(value));

        builder.Property(p => p.FullName)
            .HasMaxLength(Patient.FullNameMaxLength);

        builder.Property(p => p.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(Email.MaxLength);

        builder.Property(p => p.Cpf)
            .HasConversion(cpf => cpf!.Value, value => Cpf.Create(value).Value)
            .HasMaxLength(Cpf.Length)
            .IsFixedLength();

        builder.Ignore(p => p.IsArchived);
        builder.Ignore(p => p.DomainEvents);

        // Email is unique per psychologist, including archived patients.
        builder.HasIndex(p => new { p.PsychologistId, p.Email }).IsUnique();

        // Soft delete: archived patients are hidden unless a query calls IgnoreQueryFilters().
        builder.HasQueryFilter(p => p.ArchivedAt == null);
    }
}