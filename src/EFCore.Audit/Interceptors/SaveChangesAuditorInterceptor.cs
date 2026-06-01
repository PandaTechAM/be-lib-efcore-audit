using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EFCore.Audit.Interceptors;

public class SaveChangesAuditorInterceptor : SaveChangesInterceptor
{
   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         return base.SavingChangesAsync(eventData, result, cancellationToken);
      }

      var trackingService = eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.AddTrackedData(eventData.Context);

      return base.SavingChangesAsync(eventData, result, cancellationToken);
   }

   public override async ValueTask<int> SavedChangesAsync(
      SaveChangesCompletedEventData eventData,
      int result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         return await base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      var trackingService = eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.UpdateTrackedData();

      if (eventData.Context.Database.CurrentTransaction is null)
      {
         await trackingService.PublishAuditTrailEventDataAsync(cancellationToken);
      }

      return await base.SavedChangesAsync(eventData, result, cancellationToken);
   }

   public override InterceptionResult<int> SavingChanges(
      DbContextEventData eventData,
      InterceptionResult<int> result)
   {
      if (eventData.Context is null)
      {
         return base.SavingChanges(eventData, result);
      }

      var trackingService = eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.AddTrackedData(eventData.Context);

      return base.SavingChanges(eventData, result);
   }

   public override int SavedChanges(
      SaveChangesCompletedEventData eventData,
      int result)
   {
      if (eventData.Context is null)
      {
         return base.SavedChanges(eventData, result);
      }

      var trackingService = eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.UpdateTrackedData();

      if (eventData.Context.Database.CurrentTransaction is null)
      {
         trackingService.PublishAuditTrailEventDataAsync(CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
      }

      return base.SavedChanges(eventData, result);
   }
}