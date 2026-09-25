using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PremierLottoApi.Utilities
{
    public class PoolCacheInvalidator
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<PoolCacheInvalidator> _logger;

        public PoolCacheInvalidator(IMemoryCache cache, ILogger<PoolCacheInvalidator> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public static string BuildCacheKey( string? status)
        {
            return $"pools_{status?.Trim().ToLower() ?? "all"}";
        }
        
        public void InvalidatePoolsCache()
        {
            _cache.Remove(BuildCacheKey(null));
            _cache.Remove(BuildCacheKey("open"));
            _cache.Remove(BuildCacheKey("locked"));

            _logger.LogInformation("Pools cache invalidated.");
        }
    }
}
