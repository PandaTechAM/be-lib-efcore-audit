using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

public class TransactionAuditorInterceptor : DbTransactionInterceptor
{
   private readonly AuditTrailTrackingService _trackingService;

   public TransactionAuditorInterceptor(
      AuditTrailTrackingService trackingService)
   {
      _trackingService = trackingService;
   }

   public override async Task TransactionCommittedAsync(
      DbTransaction transaction,
      TransactionEndEventData eventData,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is not null)
      {
         _trackingService.UpdateTrackedData(eventData.Context);

         await _trackingService.PublishAuditTrailEventDataAsync(
            eventData.Context,
            cancellationToken);
      }

      await base.TransactionCommittedAsync(
         transaction,
         eventData,
         cancellationToken);
   }

   public override void TransactionCommitted(
   DbTransaction transaction,
   TransactionEndEventData eventData)
   {
      if (eventData.Context is not null)
      {
         _trackingService.UpdateTrackedData(eventData.Context);

         _trackingService.PublishAuditTrailEventDataAsync(
               eventData.Context,
               CancellationToken.None)
            .GetAwaiter()
            .GetResult();
      }

      base.TransactionCommitted(
         transaction,
         eventData);
   }
}