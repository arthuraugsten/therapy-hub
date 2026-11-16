namespace TherapyHub.BuildingBlocks.Infrastructure;

/// <summary>Configuration keys for the database. All modules share one database; each module has its own schema.</summary>
public static class DatabaseSettings
{
    /// <summary>Name under "ConnectionStrings" in configuration.</summary>
    public const string ConnectionStringName = "TherapyHub";
}