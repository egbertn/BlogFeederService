using System;
using System.Threading.Tasks;
namespace ADCCure.BlogFeederService.Interfaces
{
	public interface ISerializerService
	{
		Task<T> Derialize<T>(string key);
		Task<bool> Serialize(string key, object data, int? timeoutSeconds = default);
		Task<T> GetAndSet<T>(string key, Func<Task<object>> func, int? timeoutSeconds = default);
		Task<T> GetAndAppend<T>(string key, Func<bool> updateIf, Func<Task<object>> func, int? timeoutSeconds = default) where T: class;

		Task Set(string key, object data, int? timeoutSeconds = default);
		Task Remove(string key);
		Task<T> Get<T>(string key);
		Task KeepAlive(string key, int? newExpireSeconds = null);
	}
}