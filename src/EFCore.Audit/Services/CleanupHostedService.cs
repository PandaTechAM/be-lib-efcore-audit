using Microsoft.Extensions.Hosting;

namespace EFCore.Audit.Services;

public class CleanupHostedService : BackgroundService
{
   private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(10));

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      while (await _timer.WaitForNextTickAsync(stoppingToken))
      {
         AuditTrailTrackingHelper.ClearOutdatedTrackedEntities();
      }
   }
}