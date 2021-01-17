

using ADCCure.BlogFeederService.Interfaces;
using ADCCure.BlogFeederService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;
using StackExchange.Redis;
using System;
using System.Globalization;

namespace ADCCure.BlogFeederService
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }
		public void ConfigureServices(IServiceCollection services)
		{
			var webOptions = new WebOptions();
			Configuration.GetSection("WebOptions").Bind(webOptions);
			services.AddOptions();
		//	services.AddApplicationInsightsTelemetry();
			services.AddSingleton(Options.Create(webOptions));
			services.AddTransient<IMailProcessingService, MailProcessingService>();

			services.AddTransient<ISerializerService, SerializerService>();
			services.AddTransient<IBlobService, BlobService>();
			services.Configure<DistributedCacheConfig>(Configuration.GetSection("RedisOptions"));
				services.AddSingleton<IConnectionMultiplexer>((e) =>
			{
				var opt = e.GetRequiredService<IOptions<DistributedCacheConfig>>();
				var cacheConfig = opt.Value;
				var redisConfig = new ConfigurationOptions
				{
					ConnectRetry = cacheConfig.ConnectRetry,
					DefaultDatabase = cacheConfig.DefaultDatabase,
					EndPoints = { {cacheConfig.Endpoint, cacheConfig.EndpointPort}},
					Ssl = cacheConfig.Ssl,
					Password = cacheConfig.Password,
					ConnectTimeout = cacheConfig.ConnectTimeout
				};
				return ConnectionMultiplexer.Connect(redisConfig);
			});
			ConfigureRedisCache(services);
		}
		private void ConfigureRedisCache(IServiceCollection services)
		{
			var distributedCacheConfig = new DistributedCacheConfig();
			Configuration.GetSection("RedisOptions").Bind(distributedCacheConfig);
			if (distributedCacheConfig.UseInMemory)
			{
			//	services.AddDistributedMemoryCache();
				return;
			}

			services.AddStackExchangeRedisCache(opt => {

				var password = distributedCacheConfig.Password;
				if (password == "nil" || password == "")
				{
					password = null;
				}
				if (string.IsNullOrEmpty(password) && distributedCacheConfig.Ssl)
				{
					throw new Exception("Missing distributedcache password in configuration.");
				}
				var options = new ConfigurationOptions
				{
					Ssl = distributedCacheConfig.Ssl,
					ConnectRetry = distributedCacheConfig.ConnectRetry,
					SslProtocols = System.Security.Authentication.SslProtocols.None,
					ConnectTimeout = distributedCacheConfig.ConnectTimeout,
					DefaultDatabase = distributedCacheConfig.DefaultDatabase,
					Password = password,
					AbortOnConnectFail = true,
					EndPoints ={ {distributedCacheConfig.Endpoint, distributedCacheConfig.EndpointPort} }
				};

				opt.ConfigurationOptions = options;
			});
	}
	}
}