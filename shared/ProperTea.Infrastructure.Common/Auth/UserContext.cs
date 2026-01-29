using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ProperTea.Infrastructure.Common.Auth;

public interface IUserContext
{
    public string? UserId { get; }
    public string? Email { get; }
    public bool IsAuthenticated { get; }
}

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("sub")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
                         ?? User?.FindFirst("email")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
