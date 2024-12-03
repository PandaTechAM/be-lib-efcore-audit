using EFCore.Audit.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.Audit.Interceptors;

public class SaveChangesAuditorInterceptor(IHttpContextAccessor contextAccessor) : SaveChangesInterceptor
{
   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         return base.SavingChangesAsync(eventData, result, cancellationToken);
      }

      var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();

      if (auditTrailTrackingService is null)
      {
         return base.SavingChangesAsync(eventData, result, cancellationToken);
      }

      auditTrailTrackingService.AddTrackedData(eventData.Context);

      return base.SavingChangesAsync(eventData, result, cancellationToken);
   }

   public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
      int result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         return await base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();

      if (auditTrailTrackingService is null)
      {
         return await base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      auditTrailTrackingService.UpdateTrackedData();

      if (eventData.Context.Database.CurrentTransaction is not null)
      {
         return await base.SavedChangesAsync(eventData, result, cancellationToken);
      }

      await auditTrailTrackingService.PublishAuditTrailEventData();
      return await base.SavedChangesAsync(eventData, result, cancellationToken);
   }

   public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
   {
      if (eventData.Context is null)
      {
         return base.SavingChanges(eventData, result);
      }

      var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();

      if (auditTrailTrackingService is null)
      {
         return base.SavingChanges(eventData, result);
      }

      auditTrailTrackingService.AddTrackedData(eventData.Context);

      return base.SavingChanges(eventData, result);
   }

   public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
   {
      if (eventData.Context is null)
      {
         return base.SavedChanges(eventData, result);
      }

      var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();

      if (auditTrailTrackingService is null)
      {
         return base.SavedChanges(eventData, result);
      }


      auditTrailTrackingService.UpdateTrackedData();

      if (eventData.Context.Database.CurrentTransaction is not null)
      {
         return base.SavedChanges(eventData, result);
      }

      auditTrailTrackingService.PublishAuditTrailEventData().GetAwaiter().GetResult();

      return base.SavedChanges(eventData, result);
   }
}