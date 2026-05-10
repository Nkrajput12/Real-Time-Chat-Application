using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConnectHub.Media.API.Service.Interface;

namespace ConnectHub.Media.API.Service
{
    public class MediaCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MediaCleanupBackgroundService> _logger;

        public MediaCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<MediaCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Media Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Media Cleanup Background Service is working.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
                    await mediaService.CleanupExpiredFilesAsync();
                }

                // Run every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Media Cleanup Background Service is stopping.");
        }
    }
}
