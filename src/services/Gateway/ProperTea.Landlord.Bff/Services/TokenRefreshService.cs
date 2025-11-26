using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ProperTea.Landlord.Bff.Services;

public class TokenRefreshService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public async Task RefreshTokenAsync(CookieValidatePrincipalContext context)
    {
        await RefreshLock.WaitAsync();
        try
        {
            var currentProps = context.Properties;
            if (!IsTokenExpired(currentProps)) 
            {
                return; 
            }

            var refreshToken = currentProps.GetTokenValue(OpenIdConnectParameterNames.RefreshToken);
            if (string.IsNullOrEmpty(refreshToken)) 
                return;
            
            var client = httpClientFactory.CreateClient();
            
            var response = await client.PostAsync(
                configuration["Oidc:Authority"] + "/protocol/openid-connect/token", 
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = configuration["Oidc:ClientId"]!,
                    ["client_secret"] = configuration["Oidc:ClientSecret"]!,
                    ["refresh_token"] = refreshToken
                }));
            
            if (!response.IsSuccessStatusCode)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
            
            var responseContent = await response.Content.ReadFromJsonAsync<TokenResponse>();
            
            var tokens = new List<AuthenticationToken>
            {
                new() { Name = OpenIdConnectParameterNames.AccessToken, Value = responseContent!.AccessToken },
                new() { Name = OpenIdConnectParameterNames.RefreshToken, Value = responseContent.RefreshToken ?? refreshToken }, // Keep old if new not provided
                new() { Name = "expires_at", Value = DateTime.UtcNow.AddSeconds(responseContent.ExpiresIn)
                    .ToString("o", CultureInfo.InvariantCulture) }
            };

            currentProps.StoreTokens(tokens);
            context.ShouldRenew = true;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public bool IsTokenExpired(AuthenticationProperties props)
    {
        var expiresAtStr = props.GetTokenValue("expires_at");
        if (DateTime.TryParse(expiresAtStr, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            return expiresAt < DateTime.UtcNow.AddMinutes(2);
        }
        return true;
    }
    
    public class TokenResponse
    {
        [JsonPropertyName("access_token")] 
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 0;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = null!;

        [JsonPropertyName("refresh_expires_in")]
        public int RefreshExpiresIn { get; set; } = 0;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = null!;

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = null!;
    }
}