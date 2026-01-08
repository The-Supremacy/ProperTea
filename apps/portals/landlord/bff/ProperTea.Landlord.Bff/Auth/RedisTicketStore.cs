using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace ProperTea.Landlord.Bff.Auth
{
    public class RedisTicketStore(
        IDistributedCache cache,
        ILogger<RedisTicketStore> logger) : ITicketStore
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<RedisTicketStore> _logger = logger;
        private const string KeyPrefix = "auth-ticket:";

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = Guid.NewGuid().ToString();
            await SetAsync(key, ticket).ConfigureAwait(false);
            return key;
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            await SetAsync(key, ticket).ConfigureAwait(false);
        }

        public async Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var data = await _cache.GetAsync(KeyPrefix + key).ConfigureAwait(false);
            if (data == null)
            {
                return null;
            }

            try
            {
                return TicketSerializer.Default.Deserialize(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ticket {Key}", key);
                return null;
            }
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(KeyPrefix + key).ConfigureAwait(false);
        }

        private async Task SetAsync(string key, AuthenticationTicket ticket)
        {
            var serialized = TicketSerializer.Default.Serialize(ticket);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(8),
            };

            await _cache.SetAsync(KeyPrefix + key, serialized, options).ConfigureAwait(false);
        }
    }
}
