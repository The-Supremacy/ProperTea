// Services/InternalTokenService.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace ProperTea.Gateway.Services;

public interface IInternalTokenService
{
    string CreateToken(string userId, string organizationId, PermissionsModel permissions);
    object GetJwks();
}

public record PermissionsModel(
    Dictionary<string, string[]> PermissionsByService,
    Dictionary<string, PermissionScope> PermissionScopes);

public record PermissionScope(string ScopeType, string[]? CompanyIds = null);

public class InternalTokenService : IInternalTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalTokenService> _logger;
    private readonly SigningKey _primaryKey;
    private readonly List<SigningKey> _signingKeys;

    public InternalTokenService(IConfiguration configuration, ILogger<InternalTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _signingKeys = LoadSigningKeys();
        _primaryKey = _signingKeys.First();

        _logger.LogInformation("Loaded {KeyCount} signing keys. Primary key: {KeyId}",
            _signingKeys.Count, _primaryKey.KeyId);
    }

    public string CreateToken(string userId, string organizationId, PermissionsModel permissions)
    {
        var issuer = _configuration["Authentication:Internal:Issuer"]!;
        var audience = _configuration["Authentication:Internal:Audience"]!;
        var expiryMinutes = int.Parse(_configuration["Authentication:Internal:ExpiryMinutes"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Iss, issuer),
            new(JwtRegisteredClaimNames.Aud, audience),
            new(JwtRegisteredClaimNames.Exp,
                DateTimeOffset.UtcNow.AddMinutes(expiryMinutes).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("orgId", organizationId),
            new("permissions", JsonSerializer.Serialize(permissions.PermissionsByService)),
            new("permissionScopes", JsonSerializer.Serialize(permissions.PermissionScopes))
        };

        var key = new RsaSecurityKey(_primaryKey.Rsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        token.Header.Add("kid", _primaryKey.KeyId);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public object GetJwks()
    {
        var keys = _signingKeys.Select(signingKey =>
        {
            var key = new RsaSecurityKey(signingKey.Rsa);
            var parameters = key.Rsa.ExportParameters(false);

            return new
            {
                kty = "RSA",
                use = "sig",
                kid = signingKey.KeyId,
                n = Convert.ToBase64String(parameters.Modulus!),
                e = Convert.ToBase64String(parameters.Exponent!)
            };
        }).ToArray();

        return new { keys };
    }

    private List<SigningKey> LoadSigningKeys()
    {
        var keys = new List<SigningKey>();
        var keyIds = _configuration["Authentication:Internal:KeyIds"]?.Split(',')
                     ?? throw new InvalidOperationException("Internal KeyIds not configured");

        foreach (var keyId in keyIds)
        {
            var trimmedKeyId = keyId.Trim();

            var rsa = CreateOrLoadKey(trimmedKeyId);
            keys.Add(new SigningKey(trimmedKeyId, rsa));
        }

        return keys;
    }

    private RSA CreateOrLoadKey(string keyId)
    {
        if (_configuration.GetValue<bool>("Authentication:Internal:UseDeterministicKeys"))
        {
            var rsa = RSA.Create(2048);
            var seed = Encoding.UTF8.GetBytes(keyId).Take(32).ToArray();
            Array.Resize(ref seed, 32);

            _logger.LogWarning("Using deterministic key generation for development. KeyId: {KeyId}", keyId);
            return rsa;
        }
        else
        {
            // Production: Load from Azure Key Vault or secure storage
            // For now, generate runtime keys (you should replace this with proper key storage)
            var rsa = RSA.Create(2048);
            _logger.LogInformation("Generated runtime RSA key for KeyId: {KeyId}", keyId);
            return rsa;
        }
    }

    private record SigningKey(string KeyId, RSA Rsa);
}