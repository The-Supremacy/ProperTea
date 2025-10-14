namespace ProperTea.Identity.Service.Models;

public record AuthResponse(
    Guid UserId, 
    string Email, 
    string AccessToken
);