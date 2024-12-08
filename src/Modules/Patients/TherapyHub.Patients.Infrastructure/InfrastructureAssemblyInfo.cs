using System.Reflection;

namespace TherapyHub.Patients.Infrastructure;

public static class InfrastructureAssemblyInfo
{
    public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
}
