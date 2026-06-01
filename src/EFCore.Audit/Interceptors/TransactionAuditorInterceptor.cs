using System.Data.Common;
using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EFCore.Audit.Interceptors;

public class TransactionAuditorInterceptor : DbTransactionInterceptor
{
   public override async Task TransactionCommittedAsync(
      DbTransaction transaction,
      TransactionEndEventData eventData,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context is null)
      {
         await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
         return;
      }

      var trackingService = eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.UpdateTrackedData();

      await trackingService.PublishAuditTrailEventDataAsync(cancellationToken);

      await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
   }

   public override void TransactionCommitted(
      DbTransaction transaction,
      TransactionEndEventData eventData)
   {
      if (eventData.Context is null)
      {
         base.TransactionCommitted(transaction, eventData);
         return;
      }

      var trackingService =
         eventData.Context.GetService<AuditTrailTrackingService>();

      trackingService.UpdateTrackedData();

      trackingService.PublishAuditTrailEventDataAsync(CancellationToken.None)
                     .GetAwaiter()
                     .GetResult();

      base.TransactionCommitted(transaction, eventData);
   }
}