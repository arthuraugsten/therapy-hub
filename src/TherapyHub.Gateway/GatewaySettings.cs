namespace TherapyHub.Gateway;

/// <summary>Configuration keys and routes owned by the gateway.</summary>
public static class GatewaySettings
{
    /// <summary>YARP routes and clusters.</summary>
    public const string ReverseProxySection = "ReverseProxy";

    /// <summary>Cluster and destination IDs as declared in appsettings.json.</summary>
    public const string ApiClusterId = "api";
    public const string ApiDestinationId = "api";

    /// <summary>Override this key (env: ReverseProxy__Clusters__api__Destinations__api__Address) to point at the API.</summary>
    public const string ApiDestinationAddressKey =
        ReverseProxySection + ":Clusters:" + ApiClusterId + ":Destinations:" + ApiDestinationId + ":Address";

    public const string AuthenticationSection = "Authentication";
    public const string AuthorityKey = AuthenticationSection + ":Authority";
    public const string AudienceKey = AuthenticationSection + ":Audience";
}