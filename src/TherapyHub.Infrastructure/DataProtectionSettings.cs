namespace TherapyHub.Infrastructure;

/// <summary>Configuration keys and defaults for ASP.NET Core Data Protection (keys that encrypt clinical content).</summary>
public static class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    /// <summary>Production: Azure Blob Storage URI where the key ring is stored.</summary>
    public const string BlobUriKey = SectionName + ":BlobUri";

    /// <summary>Production: Azure Key Vault key URI that wraps the key ring.</summary>
    public const string KeyVaultKeyUriKey = SectionName + ":KeyVaultKeyUri";

    /// <summary>Non-production: folder for the key ring. Defaults to <see cref="DefaultKeysFolder"/> under LocalApplicationData.</summary>
    public const string KeysPathKey = SectionName + ":KeysPath";

    /// <summary>Isolates this app's keys. Changing it makes existing encrypted data unreadable.</summary>
    public const string ApplicationName = "TherapyHub";

    public const string DefaultKeysFolder = "DataProtection-Keys";
}