using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace ProperTea.Landlord.Bff.Services;

public class RedisTicketStore(IDistributedCache cache) : ITicketStore
{
    private const string KeyPrefix = "AuthSession-";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sid = ticket.Principal.FindFirstValue("sid");
        if (string.IsNullOrEmpty(sid))
        {
            throw new InvalidOperationException("Session ID (sid) claim is missing. Cannot store ticket.");
        }

        var key = $"{KeyPrefix}{sid}";
        await RenewAsync(key, ticket).ConfigureAwait(false);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(expiresUtc.Value);
        }

        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        await cache.SetAsync(key, ticketBytes, options).ConfigureAwait(false);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var ticketBytes = await cache.GetAsync(key).ConfigureAwait(false);
        return ticketBytes == null ? null : TicketSerializer.Default.Deserialize(ticketBytes);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key).ConfigureAwait(false);
    }
}
