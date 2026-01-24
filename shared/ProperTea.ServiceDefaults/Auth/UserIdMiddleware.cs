using System.Security.Claims;
using Marten;

namespace ProperTea.ServiceDefaults.Auth
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
