using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProperTea.Landlord.Bff.Endpoints;

public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }))
            .WithName("Login");

        app.MapGet("/logout", (HttpContext context) =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return Results.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
            }

            return Results.Redirect("/");
        }).WithName("Logout");

        app.MapPost("/backchannel-logout", async (
            HttpContext context,
            ITicketStore ticketStore,
            IServiceProvider services) =>
        {
            var form = await context.Request.ReadFormAsync().ConfigureAwait(false);
            var logoutToken = form["logout_token"].FirstOrDefault();
            if (logoutToken is null)
            {
                return Results.BadRequest("No logout_token received.");
            }

            var oidcOptions = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);
            var oidcConfig =
                await oidcOptions.ConfigurationManager!.GetConfigurationAsync(context.RequestAborted)
                    .ConfigureAwait(false);

            var config = services.GetRequiredService<IConfiguration>();
            var clientId = config["Oidc:ClientId"];

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = oidcConfig.Issuer,
                ValidAudience = clientId,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateLifetime = false
            };

            var handler = new JwtSecurityTokenHandler();
            ClaimsIdentity principal;
            try
            {
                var result = await handler.ValidateTokenAsync(logoutToken, validationParameters).ConfigureAwait(false);
                principal = result.ClaimsIdentity;
            }
            catch (Exception)
            {
                return Results.BadRequest("Invalid logout token.");
            }

            var sid = principal.Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
            if (sid is null)
            {
                return Results.BadRequest("logout_token is missing 'sid' claim.");
            }

            var redisKey = $"AuthSession-{sid}";
            await ticketStore.RemoveAsync(redisKey).ConfigureAwait(false);
            return Results.Ok();
        }).AllowAnonymous();
    }
}
