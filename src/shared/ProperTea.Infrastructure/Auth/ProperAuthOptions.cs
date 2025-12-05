namespace ProperTea.Infrastructure.Auth;

public class ProperAuthOptions
{
    public const string SectionName = "ProperTeaAuth";

    public string Authority { get; set; } = string.Empty;
    public string InternalMetadataAddress { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool RequireHttps { get; set; } = true;
}
