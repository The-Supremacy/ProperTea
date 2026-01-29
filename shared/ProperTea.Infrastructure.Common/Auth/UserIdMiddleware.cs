using System.Security.Claims;
using Marten;

namespace ProperTea.Infrastructure.Common.Auth
{
    public class UserIdMiddleware
    {
        public void Before(IDocumentSession session, ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated == true)
            {
                session.LastModifiedBy = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }
    }
}
