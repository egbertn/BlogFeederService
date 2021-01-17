using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ADCCure.BlogFeederService.Interfaces;
using ADCCure.BlogFeederService.Models;
using StackExchange.Redis;

namespace ADCCure.BlogFeederService
{
	public class SerializerService : ISerializerService
	{
		private readonly IConnectionMultiplexer _cache;
		private readonly DistributedCacheConfig _config;
		private readonly ILogger<SerializerService> _logger;
		private static readonly Lazy<JsonSerializerOptions> Options = new Lazy<JsonSerializerOptions>(() => new JsonSerializerOptions(JsonSerializerDefaults.Web) { IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
		public SerializerService(IConnectionMultiplexer cache,
			IOptions<DistributedCacheConfig> config,
			ILogger<SerializerService> logger)
		{
			_config = config == null ? new DistributedCacheConfig { DataTimeOut = 60 } : config.Value;
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_logger = logger ?? new NullLogger<SerializerService>();
		}
		private IDatabase GetDatabase()
		{
			return _cache.GetDatabase();
		}
		public async Task<T> Derialize<T>(string key)
		{
			var data = await GetDatabase().StringGetAsync(key);
			_logger.LogInformation("trying cache with key {0}", key);
			if (data.IsNullOrEmpty)
			{
				_logger.LogInformation("key {0} was not cached", key);
				return default;
			}
			try
			{
				var result = JsonSerializer.Deserialize<T>(data, Options.Value);
				return result;
			}
			catch(Exception ex)
			{
				_logger.LogError("Deserialize failed: {0}", ex.Message);
				return default;
			}
		}
		public async Task<T> GetAndAppend<T>(string key, Func<bool> updateIf, Func<Task<object>> func, int? expireSeconds) where T :class
		{
			//after the key:timecreated=unixtimeinseconds
			//    key will make the data stale
			var data = await GetDatabase().StringGetAsync(key);

			bool stale = false;

			_logger.LogInformation("getting from cache key {0}", key);
			DeepCopyHelper.IsCollection(typeof(T), out Type inner);
			T old;
			if (!data.IsNullOrEmpty)
			{
				var created = JsonSerializer.Deserialize<CreatedKey<T>>(data, Options.Value);
				old = created.Data;
				stale = created.Stale;
			}
			else
			{
				old = Array.CreateInstance(inner, 0) as T;//empty arry
			}
			if (stale) // no appending anymore
			{
				return old;
			}
			var o = await func.Invoke();
			var isCollection = (o.GetType() == typeof(T) || DeepCopyHelper.IsCollectionGenericTypeEqual(o.GetType(), typeof(T)));
			// we can only compare if we have data
			if ( o != null && isCollection) // e.g. StatusCodeResult
			{
				//var listType = typeof(List<>).MakeGenericType(inner);
				var newList = (o as IList);
				var oldList = (old as IList);
				var newArray = Array.CreateInstance(inner, oldList.Count + newList.Count);
				//
				oldList.CopyTo(newArray, 0);
				newList.CopyTo(newArray, oldList.Count);
				//append and serialize
				var createdKey = new CreatedKey<T>
				{
					Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
					Stale = updateIf.Invoke(),
					Data = newArray as T

				};
				await Serialize(key, createdKey, expireSeconds);
				return newArray as T;
			}
			//JUST return the data
			return old; // return the cached data
		}
		public async Task<T> GetAndSet<T>(string key, Func<Task<object>> func, int? expireSeconds)
		{
			var data = await GetDatabase().StringGetAsync(key);
			_logger.LogInformation("getting from cache key {0}", key);
			if (data.IsNullOrEmpty)
			{
				var o = await func.Invoke();
				if (o != null && (o.GetType() == typeof(T) || DeepCopyHelper.IsCollectionGenericTypeEqual(o.GetType(), typeof(T)))) // e.g. StatusCodeResult
				{
					await Serialize(key, o, expireSeconds);
					return (T)o;
				}
				return default;
			}

			var result = JsonSerializer.Deserialize<T>(data, Options.Value);
			return result;
		}
		public  Task Set(string key, object data, int? expireSeconds)
		{
			var ct = 0;
			if (data is IList dt)
				ct = dt.Count;
			if (data is Array ar)
				ct = ar.Length;
			_logger.LogInformation("setting cache key {0} count = ({1})", key, ct);
			if (data != null)
			{
				return Serialize(key, data, expireSeconds);
			}
			return Task.CompletedTask;
		}
		public async Task<T> Get<T>(string key)
		{
			var data = await GetDatabase().StringGetAsync(key);
			_logger.LogInformation("getting from cache key {0}", key);
			if (!data.IsNullOrEmpty)
			{
				var result = await JsonSerializer.DeserializeAsync<T>(new MemoryStream(data), Options.Value);
				return result;
			}
			return default;
		}
		public Task Remove(string key)
		{
			return GetDatabase().KeyDeleteAsync(key);
		}

		/// <summary>
		/// caches the given data by key
		/// If Redis or Memory cache fails,
		/// it will ignore the exception and your
		/// operation will continue, without cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="data"></param>
		/// <param name="expireSeconds"></param>
		public async Task<bool> Serialize(string key, object data, int? expireSeconds = default)
		{
			_logger.LogInformation("setting data with cache key {0}", key);
			var bytes = JsonSerializer.SerializeToUtf8Bytes(data, Options.Value);
			try
			{
				await GetDatabase().StringSetAsync(key, bytes, expiry: TimeSpan.FromSeconds(expireSeconds ?? _config.DataTimeOut) );
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError("Serialize failed: {0}", ex.Message);
				return false;
			}
		}

		public async Task KeepAlive(string key, int? newExpireSeconds = default)
		{
			// could be done more efficient, though, we don't want to address redis directly
			// the scope of this service is that it 'could be in-memory' too
			//return _cache.RefreshAsync(key);
			var data = await GetDatabase().StringGetAsync(key);
			if (!data.IsNullOrEmpty)
			{
				await GetDatabase().StringSetAsync(key, data, expiry: TimeSpan.FromSeconds(newExpireSeconds ?? _config.DataTimeOut) );
			}
		}
	}
}