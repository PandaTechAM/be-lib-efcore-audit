using System.Data.Common;
using EFCore.Audit.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.Audit.Interceptors;

public class TransactionAuditorInterceptor(IHttpContextAccessor contextAccessor) : DbTransactionInterceptor
{
   public override async Task TransactionCommittedAsync(DbTransaction transaction,
      TransactionEndEventData eventData,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context?.Database.CurrentTransaction is not null)
      {
         var auditTrailTrackingService = contextAccessor.GetAuditTrailTrackingService();
         if (auditTrailTrackingService is null)
         {
            await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
            return;

         }

         auditTrailTrackingService.UpdateTrackedData();
         await auditTrailTrackingService.PublishAuditTrailEventData();
      }

      await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
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
         auditTrailTrackingService.PublishAuditTrailEventData().GetAwaiter().GetResult();
      }

      base.TransactionCommitted(transaction, eventData);
   }
}