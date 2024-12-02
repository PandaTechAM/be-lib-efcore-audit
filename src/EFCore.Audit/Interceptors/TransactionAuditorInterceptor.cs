using System.Data.Common;
using EFCore.Audit.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.Audit.Interceptors;

public class TransactionAuditorInterceptor(IHttpContextAccessor contextAccessor) : DbTransactionInterceptor
{
   public override Task TransactionCommittedAsync(DbTransaction transaction,
      TransactionEndEventData eventData,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context?.Database.CurrentTransaction is not null)
      {
         var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();
         if (auditTrailTrackingService is null)
         {
            base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
            return Task.CompletedTask;

         }

         auditTrailTrackingService.UpdateTrackedData();
         auditTrailTrackingService.PublishAuditTrailEventData();
      }

      base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
      return Task.CompletedTask;
   }

   public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
   {
      if (eventData.Context?.Database.CurrentTransaction is not null)
      {
         var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();
         if (auditTrailTrackingService is null)
         {
            base.TransactionCommitted(transaction, eventData);
            return;
         }

         auditTrailTrackingService.UpdateTrackedData();
         auditTrailTrackingService.PublishAuditTrailEventData();
      }

      base.TransactionCommitted(transaction, eventData);
   }
}