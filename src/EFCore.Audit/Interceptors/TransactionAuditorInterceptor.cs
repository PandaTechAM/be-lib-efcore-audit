using System.Data.Common;
using EFCore.Audit.Configurator;
using EFCore.Audit.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.Audit.Interceptors;

public class TransactionAuditorInterceptor(AuditTrailConfigurator auditTrailConfigurator) : DbTransactionInterceptor
{
   public override async Task TransactionCommittedAsync(DbTransaction transaction,
      TransactionEndEventData eventData,
      CancellationToken cancellationToken = default)
   {
      if (eventData.Context?.Database.CurrentTransaction is not null)
      {
         AuditTrailTrackingHelper.UpdateTrackedData(eventData.Context);
         AuditTrailTrackingHelper.PublishAuditTrailEventData(eventData.Context.ContextId.InstanceId, auditTrailConfigurator);
      }

      await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
   }

   public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
   {
      if (eventData.Context?.Database.CurrentTransaction is not null)
      {
         AuditTrailTrackingHelper.UpdateTrackedData(eventData.Context);
         AuditTrailTrackingHelper.PublishAuditTrailEventData(eventData.Context.ContextId.InstanceId, auditTrailConfigurator);
      }

      base.TransactionCommitted(transaction, eventData);
   }
}