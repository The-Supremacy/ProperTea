namespace ProperTea.Identity.Api.Configuration;

public class IdentitySettings
{
    public bool RequireConfirmedAccount { get; set; }
    public PasswordSettings Password { get; set; } = new();
}

public class PasswordSettings
{
    public bool RequireDigit { get; set; } = true;
    public int RequiredLength { get; set; } = 8;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
}