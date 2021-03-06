using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ADCCure.BlogFeederService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)              
                .ConfigureServices((hostContext, services) =>
                {
                    var startup = new Startup(hostContext.Configuration);
                    startup.ConfigureServices(services);
                    services.AddHostedService<Worker>();
                });
    }
}
