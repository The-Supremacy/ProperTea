using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

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

        app.MapPost("/backchannel-logout", async (HttpContext context, ITicketStore ticketStore) =>
        {
            var form = await context.Request.ReadFormAsync();
            var logoutToken = form["logout_token"].FirstOrDefault();
            if (logoutToken is null)
            {
                return Results.BadRequest("No logout_token received.");
            }

            var handler = new JwtSecurityTokenHandler();
            if (handler.ReadToken(logoutToken) is not JwtSecurityToken token)
            {
                return Results.BadRequest("Invalid logout_token.");
            }

            var sid = token.Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
            if (sid is null)
            {
                return Results.BadRequest("logout_token is missing 'sid' claim.");
            }

            // Construct the key that was used to store the ticket in RedisTicketStore
            var redisKey = $"AuthSession-{sid}";
            await ticketStore.RemoveAsync(redisKey);
            return Results.Ok();
        }).AllowAnonymous();
    }
}