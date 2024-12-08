namespace TherapyHub.Patients.Domain;

public sealed class Patient
{
    private Patient(string name)
    {
        Name = name;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }

    public static Patient Create(string name)
    {
        return new Patient(name);
    }
}
