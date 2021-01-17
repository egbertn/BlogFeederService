using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ADCCure.BlogFeederService.Interfaces;
namespace ADCCure.BlogFeederService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMailProcessingService _mailServices;

        public Worker(ILogger<Worker> logger, IMailProcessingService mailProcessingService)
        {
            _logger = logger;
            _mailServices = mailProcessingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await _mailServices.PollEmailMessages(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
