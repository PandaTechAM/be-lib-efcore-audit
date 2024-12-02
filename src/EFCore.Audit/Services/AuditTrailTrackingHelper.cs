using System.Collections.Concurrent;
using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Services;

internal static class AuditTrailTrackingHelper
{
   private static readonly ConcurrentDictionary<Guid, AuditTrackingData> PendingTrackedEntities = [];

   internal static void AddTrackedData(DbContext? dbContext)
   {
      if (dbContext is null)
      {
         return;
      }

      var entries = dbContext.ChangeTracker
                             .Entries()
                             .ToList();

      var auditTrackData = PendingTrackedEntities.GetOrAdd(
         dbContext.ContextId.InstanceId,
         _ => new AuditTrackingData()
      );

      foreach (var entry in entries)
      {
         var entityType = entry.Entity.GetType();
         var auditedEntity = new AuditedEntity
         {
            Entry = entry,
            ActionType = entry.State.ToAuditActionType(),
            Name = entityType.Name,

            PrimaryKeyValue = entry.GetPrimaryKeyValue(),
            Type = entityType
         };

         foreach (var property in entry.Properties)
         {
            auditedEntity.TrackedProperty[property.Metadata.Name] = property.CurrentValue;
         }

         lock (auditTrackData) //todo Review if this is correct
         {
            auditTrackData.Entities.Add(auditedEntity);
         }
      }
   }

   internal static void UpdateTrackedData(DbContext? dbContext) //todo Do I need lock here
   {
      if (dbContext is null)
      {
         return;
      }

      if (!PendingTrackedEntities.TryGetValue(dbContext.ContextId.InstanceId, out var trackingData))
      {
         return;
      }

      foreach (var entity in trackingData.Entities)
      {
         entity.PrimaryKeyValue = entity.Entry.GetPrimaryKeyValue();

         foreach (var property in entity.Entry.Properties)
         {
            entity.TrackedProperty[property.Metadata.Name] = property.CurrentValue;
         }
      }
   }

   internal static void ClearOutdatedTrackedEntities()
   {
      var expirationTime = DateTime.Now.AddMinutes(-5);

      foreach (var kvp in PendingTrackedEntities)
      {
         if (kvp.Value.TrackedAt < expirationTime)
         {
            PendingTrackedEntities.TryRemove(kvp.Key, out _);
         }
      }
   }


   internal static void PublishAuditTrailEventData(Guid contextInstanceId, AuditTrailConfigurator configurator)
   {
      var auditTrailEventData = ProcessTrackedData(contextInstanceId, configurator);

      if (auditTrailEventData is null || auditTrailEventData.Entities.Count == 0)
      {
         return;
      }

      AuditEventBroker.Publish(auditTrailEventData);
   }

   private static AuditTrailEventData? ProcessTrackedData(Guid contextInstanceId, AuditTrailConfigurator configurator)
   {
      if (!PendingTrackedEntities.TryGetValue(contextInstanceId, out var auditTrackData))
      {
         return null;
      }

      var transformedEntities = new List<AuditTrailEventEntity>();

      foreach (var entity in auditTrackData.Entities)
      {
         if (!entity.ActionType.IsInAuditScope())
         {
            continue;
         }

         var entityConfig = configurator.GetEntityConfiguration(entity.Type);

         if (entityConfig is null)
         {
            continue;
         }

         if (entityConfig.AuditActions is not null && !entityConfig.AuditActions.Contains(entity.ActionType))
         {
            continue;
         }

         Dictionary<string, object?> transformedProperties = new();

         foreach (var (propertyName, propertyValue) in entity.TrackedProperty)
         {
            if (entityConfig.Properties.TryGetValue(propertyName, out var propertyConfig))
            {
               if (propertyConfig.Ignore)
               {
                  continue;
               }

               var transformedValue = propertyConfig.Transform?.Invoke(propertyValue) ?? propertyValue;

               transformedProperties.Add(propertyName, transformedValue);
            }
            else
            {
               transformedProperties.Add(propertyName, propertyValue);
            }
         }

         var transformedEntity = new AuditTrailEventEntity(entityConfig.ServiceName,
            entity.ActionType,
            entity.Name,
            entityConfig.PermissionToRead,
            entity.PrimaryKeyValue,
            transformedProperties);

         transformedEntities.Add(transformedEntity);
      }

      ClearTrackedEntitiesByInstanceId(contextInstanceId);
      return new AuditTrailEventData(transformedEntities);
   }

   private static void ClearTrackedEntitiesByInstanceId(Guid instanceId)
   {
      PendingTrackedEntities.TryRemove(instanceId, out _);
   }

   private static string GetPrimaryKeyValue(this EntityEntry entry)
   {
      return string.Join("_",
         entry.Properties
              .Where(p => p.Metadata.IsPrimaryKey())
              .Select(p => p.CurrentValue?.ToString() ?? string.Empty));
   }
}