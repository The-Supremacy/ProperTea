namespace ProperTea.Identity.Service.DTOs;

public record AuthResponse(
    Guid UserId,
    string Email,
    string AccessToken
);