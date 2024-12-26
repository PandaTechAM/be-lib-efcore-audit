using EFCore.Audit.Configurator;
using EFCore.Audit.Helpers;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;

namespace EFCore.Audit.Services.Implementations;

internal class AuditTrailPublisher(IAuditTrailConsumer auditTrailConsumer, AuditTrailConfigurator configurator)
   : IAuditTrailPublisher
{
   public async Task BulkAuditAsync(List<ManualAuditEntry> auditEntries)
   {
      if (auditEntries.Count == 0)
      {
         return;
      }

      var transformedEntities = new List<AuditTrailEventEntity>();

      foreach (var auditEntry in auditEntries)
      {
         if (!AuditTrailHelper.ShouldProcessEntity(
                auditEntry.Action,
                auditEntry.EntityType,
                configurator,
                out var entityConfig))
         {
            continue;
         }

         foreach (var detail in auditEntry.ChangedItems)
         {
            var primaryKeyValue = string.Join("_", detail.PrimaryKeyIds);

            var transformedProps = AuditTrailHelper.TransformProperties(
               detail.ChangedProperties,
               entityConfig!.Properties
            );

            var eventEntity = new AuditTrailEventEntity(
               null,
               entityConfig.ServiceName,
               auditEntry.Action,
               auditEntry.EntityType.Name,
               entityConfig.PermissionToRead,
               primaryKeyValue,
               transformedProps
            );

            transformedEntities.Add(eventEntity);
         }
      }

      if (transformedEntities.Count == 0)
      {
         return;
      }

      var eventData = new AuditTrailEventData(transformedEntities);
      await auditTrailConsumer.ConsumeAuditTrailAsync(eventData);
   }
}