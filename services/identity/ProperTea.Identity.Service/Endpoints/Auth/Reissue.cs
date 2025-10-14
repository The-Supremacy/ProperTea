using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Identity.Service.Models;
using ProperTea.Identity.Service.Services;

namespace ProperTea.Identity.Service.Endpoints.Auth;

public static class Reissue
{
    public static IEndpointRouteBuilder MapReissueEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reissue", async (
                ReissueRequest request,
                UserManager<ProperTeaUser> userManager,
                ITokenService tokenService,
                IConfiguration configuration) =>
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["Secret"];

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                };

                try
                {
                    var principal = tokenHandler.ValidateToken(request.ExpiredToken, validationParameters, out _);
                    var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                        return Results.Unauthorized();

                    var user = await userManager.FindByIdAsync(userId.ToString());

                    if (user is null)
                        return Results.Unauthorized();

                    var newToken = tokenService.CreateToken(user);
                    var response = new AuthResponse(user.Id, user.Email!, newToken);

                    return Results.Ok(response);
                }
                catch (SecurityTokenException)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("ReissueToken")
            .WithSummary("Reissues an access token")
            .WithDescription("Provides a new access token if a valid but expired token is provided.")
            .Produces<AuthResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}