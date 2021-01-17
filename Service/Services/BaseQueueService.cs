using StackExchange.Redis;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ADCCure.BlogFeederService.Models;

namespace ADCCure.BlogFeederService.Services
{
public abstract class BaseQueueService
	{
		private const string DeadQueueName = "_DEAD_QUEUE";
		private readonly IConnectionMultiplexer _connectionMultiplexer;
		public BaseQueueService(IConnectionMultiplexer connection)
		{
			_connectionMultiplexer = connection;
		}
		private static JsonSerializerOptions JsonSerializerOptions()
		{
			var conv = new JsonSerializerOptions { IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
			conv.Converters.Add(new JsonStringEnumConverter());
			return conv;
		}
		protected async Task AddMessageToQueue(string queueName, object data)
		{
			var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options: JsonSerializerOptions());
			var queue = queueName;
			if (data is Message msg && msg.IsFromDeadQueue)
			{
				queue += DeadQueueName;
			}
			await GetDatabase().ListLeftPushAsync(queue, jsonBytes, flags: CommandFlags.FireAndForget);
		}
		protected Task AddMessageToList(string listName, object data)
		{
			return AddMessageToList( listName, data, System.DateTimeOffset.UtcNow);
		}
		protected async Task AddMessageToList(string listName, object data, System.DateTimeOffset timestamp)
		{
			var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options: JsonSerializerOptions());
			await GetDatabase().SortedSetAddAsync(listName, jsonBytes, score:timestamp.ToUnixTimeSeconds(), flags: CommandFlags.FireAndForget);
		}
		protected Task<IEnumerable<T>> GetMessagesFromList<T>(string listName, DateTimeOffset fromRange, DateTimeOffset toRange) where T: Message
		{
			return GetMessagesFromList<T>(listName, fromRange.ToUnixTimeSeconds(), toRange.ToUnixTimeSeconds());
		}
		protected async Task<IEnumerable<T>> GetMessagesFromList<T>(string listName, long fromRange = long.MinValue, long toRange= long.MaxValue) where T: Message
		{
			var values = await GetDatabase().SortedSetRangeByScoreAsync(listName, fromRange, toRange);
			var arr = values.Select(s => JsonSerializer.Deserialize<T>(s, JsonSerializerOptions())).ToArray();
			return arr;
		}
		protected Task<long> RemoveMessagesFromList(string listName, DateTimeOffset fromRange , DateTimeOffset toRange)
		{
			return RemoveMessagesFromList(listName, fromRange.ToUnixTimeSeconds(), toRange.ToUnixTimeSeconds());
		}
		protected async Task<long> RemoveMessagesFromList(string listName, long fromRange = long.MinValue, long toRange = long.MaxValue)
		{
			var deleted = await GetDatabase().SortedSetRemoveRangeByScoreAsync(listName, fromRange, toRange, flags: CommandFlags.FireAndForget);
			return deleted;
		}
		protected async Task<T> RemoveMessageFromQueue<T>(string queueName) where T : Message
		{
			var db = GetDatabase();
			var data = await db.ListRightPopAsync(queueName);
			var isfromDeadQueue = false;
			//if (!data.HasValue) // try dead queue
			//{
			//	data = await db.ListRightPopAsync(queueName + DeadQueueName);
			//	isfromDeadQueue = true;
			//}
			if (data.HasValue)
			{
				var retVal = JsonSerializer.Deserialize<T>((byte[])data, options: JsonSerializerOptions());
				if (isfromDeadQueue)
				{
					retVal.IsFromDeadQueue = true;
				}
				return retVal;
			}

			return default;
		}
		private IDatabase GetDatabase()
		{
			return _connectionMultiplexer.GetDatabase();
		}
	}
}