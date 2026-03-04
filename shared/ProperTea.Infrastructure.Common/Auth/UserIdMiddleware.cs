using System.Security.Claims;
using Marten;
using Microsoft.AspNetCore.Http;

namespace ProperTea.Infrastructure.Common.Auth
{
    public class UserIdMiddleware
    {
        public void Before(IDocumentSession session, IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                session.LastModifiedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }
    }
}
