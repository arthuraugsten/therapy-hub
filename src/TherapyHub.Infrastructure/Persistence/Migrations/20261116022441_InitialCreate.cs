using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TherapyHub.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261116022441_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PsychologistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                Cpf = table.Column<string>(type: "nchar(11)", fixedLength: true, maxLength: 11, nullable: true),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Patients_PsychologistId_Email",
            table: "Patients",
            columns: new[] { "PsychologistId", "Email" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Patients");
    }
}