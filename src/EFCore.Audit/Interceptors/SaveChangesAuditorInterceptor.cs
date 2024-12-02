using EFCore.Audit.Configurator;
using EFCore.Audit.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.Audit.Interceptors;

public class SaveChangesAuditorInterceptor(AuditTrailConfigurator configurator) : SaveChangesInterceptor
{
   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
   {
      AuditTrailTrackingHelper.AddTrackedData(eventData.Context);

      return base.SavingChangesAsync(eventData, result, cancellationToken);
   }

   public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
      int result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         return base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      AuditTrailTrackingHelper.UpdateTrackedData(eventData.Context);

      if (eventData.Context.Database.CurrentTransaction is not null)
      {
         return base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      AuditTrailTrackingHelper.PublishAuditTrailEventData(eventData.Context.ContextId.InstanceId, configurator);
      return base.SavedChangesAsync(eventData, result, cancellationToken);
   }

   public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
   {
      AuditTrailTrackingHelper.AddTrackedData(eventData.Context);

      return base.SavingChanges(eventData, result);
   }

   public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
   {
      if (eventData.Context is null)
      {
         return base.SavedChanges(eventData, result);
      }

      AuditTrailTrackingHelper.UpdateTrackedData(eventData.Context);

      if (eventData.Context.Database.CurrentTransaction is not null)
      {
         return base.SavedChanges(eventData, result);
      }

      AuditTrailTrackingHelper.PublishAuditTrailEventData(eventData.Context.ContextId.InstanceId, configurator);

      return base.SavedChanges(eventData, result);
   }
}