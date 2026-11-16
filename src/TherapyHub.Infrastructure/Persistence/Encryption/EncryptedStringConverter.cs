using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TherapyHub.Infrastructure.Persistence.Encryption;

/// <summary>
/// Encrypts a string column with ASP.NET Core Data Protection. Domain sees plain text; the database stores ciphertext.
/// Encrypted columns cannot be filtered, sorted, or searched in SQL.
/// </summary>
internal sealed class EncryptedStringConverter(IDataProtector protector)
    : ValueConverter<string, string>(
        plainText => protector.Protect(plainText),
        cipherText => protector.Unprotect(cipherText));

internal static class EncryptionPropertyBuilderExtensions
{
    /// <summary>Purpose string for clinical content. Changing it makes existing data unreadable.</summary>
    public const string ClinicalContentPurpose = "TherapyHub.ClinicalContent.v1";

    /// <summary>
    /// Usage in a configuration: <c>builder.Property(n => n.Content).IsEncrypted(protector);</c>.
    /// The protector comes from <c>IDataProtectionProvider.CreateProtector(ClinicalContentPurpose)</c>.
    /// </summary>
    public static PropertyBuilder<string> IsEncrypted(this PropertyBuilder<string> property, IDataProtector protector) =>
        property.HasConversion(new EncryptedStringConverter(protector));
}