using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProperTea.Core.Auth;

namespace ProperTea.Infrastructure.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? Id => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => !string.IsNullOrEmpty(Id);
}
