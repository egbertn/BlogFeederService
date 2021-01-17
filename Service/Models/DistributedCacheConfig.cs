namespace ADCCure.BlogFeederService
{
	public class DistributedCacheConfig
	{
		/// <summary>
		/// Flag if a in-memory cache should be used instead of Redis.
		/// Only works in debug mode
		/// </summary>
		public bool UseInMemory { get; set; }
		public string Endpoint { get; set; }
		public int? DefaultDatabase { get; set; }
		public int EndpointPort { get; set; }
		public bool Ssl { get; set; }
		public int ConnectRetry { get; set; }
		public int ConnectTimeout { get; set; } = 5000;
		public string Password { get; set; }
		/// <summary>
		/// default Data caching in seconds
		/// </summary>
		public int DataTimeOut { get; set; }
	}
}