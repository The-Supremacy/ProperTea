namespace ProperTea.Identity.Api.DTOs;

public record AuthResponse(
    Guid UserId,
    string Email,
    string AccessToken
);