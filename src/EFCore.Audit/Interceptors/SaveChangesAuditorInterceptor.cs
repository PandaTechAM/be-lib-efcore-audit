using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class SaveChangesAuditorInterceptor : SaveChangesInterceptor
{
   private readonly AuditTrailTrackingService _trackingService;

   public SaveChangesAuditorInterceptor(
      AuditTrailTrackingService trackingService)
   {
      _trackingService = trackingService;
   }

   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is not null)
      {
         _trackingService.AddTrackedData(eventData.Context);
      }

      return base.SavingChangesAsync(
         eventData,
         result,
         cancellationToken);
   }

   public override async ValueTask<int> SavedChangesAsync(
      SaveChangesCompletedEventData eventData,
      int result,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is not null)
      {
         _trackingService.UpdateTrackedData(eventData.Context);

         if (eventData.Context.Database.CurrentTransaction is null)
         {
            await _trackingService.PublishAuditTrailEventDataAsync(
               eventData.Context,
               cancellationToken);
         }
      }

      return await base.SavedChangesAsync(
         eventData,
         result,
         cancellationToken);
   }

   public override InterceptionResult<int> SavingChanges(
   DbContextEventData eventData,
   InterceptionResult<int> result)
   {
      if (eventData.Context is not null)
      {
         _trackingService.AddTrackedData(eventData.Context);
      }

      return base.SavingChanges(
         eventData,
         result);
   }

   public override int SavedChanges(
      SaveChangesCompletedEventData eventData,
      int result)
   {
      if (eventData.Context is not null)
      {
         _trackingService.UpdateTrackedData(eventData.Context);

         if (eventData.Context.Database.CurrentTransaction is null)
         {
            _trackingService.PublishAuditTrailEventDataAsync(
                  eventData.Context,
                  CancellationToken.None)
               .GetAwaiter()
               .GetResult();
         }
      }

      return base.SavedChanges(
         eventData,
         result);
   }
}