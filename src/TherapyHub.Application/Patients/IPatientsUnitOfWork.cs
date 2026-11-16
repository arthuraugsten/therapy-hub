using TherapyHub.BuildingBlocks.Application.Abstractions.Data;

namespace TherapyHub.Application.Patients;

/// <summary>Saves the Patients module's changes. Each module has its own unit of work (its own DbContext).</summary>
public interface IPatientsUnitOfWork : IUnitOfWork;