namespace TherapyHub.Api.Authentication;

/// <summary>Configuration keys for JWT bearer validation against the Keycloak realm.</summary>
public static class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>Keycloak realm URL, e.g. http://localhost:8080/realms/therapy-hub.</summary>
    public const string AuthorityKey = SectionName + ":Authority";

    /// <summary>Expected "aud" claim (therapy-hub-api).</summary>
    public const string AudienceKey = SectionName + ":Audience";
}