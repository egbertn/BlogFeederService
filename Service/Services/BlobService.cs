using ADCCure.BlogFeederService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ADCCure.BlogFeederService.Services
{
    public class BlobService : IBlobService
    {
        private readonly IConnectionMultiplexer _distributedCache;
        private readonly ILogger<BlobService> _logger;
        public BlobService(IConnectionMultiplexer distributedCache,
                ILogger<BlobService> logger,
                IOptions<WebOptions> options)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }
        private IDatabase GetDatabase()
        {
            return _distributedCache.GetDatabase();
        }
        public async Task<byte[]> GetBlob(string id)
        {

            _logger.LogInformation("getting blob with id {0}", id);
            var result = await GetDatabase().StringGetAsync(id);
            if (result.IsNullOrEmpty)
            {
                return result;
            }
            _logger.LogWarning("blob id {0} is empty", id);
            return default;
        }

        private static string HashCode(byte[] content)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(content);
            return Convert.ToHexString(bytes);
        }
        /// <summary>
        /// Stores a blob with expiration time as 1 day
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="key"></param>
        public async Task<string> StoreBlob(byte[] blob, Func<string, string> key = default, int? expirySeconds = null)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            var hashcode = HashCode(blob);
            if (key != null)
            {
                hashcode = key.Invoke(hashcode);
            }
            _logger.LogInformation("storing blob with id {0}", hashcode);
            try
            {
                // note for a blob, mostly image, it is no problem to be stale
                await GetDatabase().StringSetAsync(hashcode, blob, expiry: expirySeconds != null ? TimeSpan.FromSeconds(expirySeconds.Value) : null);
            }
            catch (Exception ex)
            {
                _logger.LogError("StoreBlob failed with {0}", ex.Message);
                return default;
            }
            return hashcode;
        }

        public async Task RemoveBlob(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            await GetDatabase().KeyDeleteAsync(id);
        }
    }
}