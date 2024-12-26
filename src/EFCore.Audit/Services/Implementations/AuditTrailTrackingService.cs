using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Helpers;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Services.Implementations;

internal class AuditTrailTrackingService(AuditTrailConfigurator configurator, IAuditTrailConsumer consumer)
{
   private readonly List<AuditedEntity> _entities = [];

   internal void AddTrackedData(DbContext? dbContext)
   {
      if (dbContext is null)
      {
         return;
      }

      var entries = dbContext.ChangeTracker
                             .Entries()
                             .ToList();


      foreach (var entry in entries)
      {
         var entityType = entry.Entity.GetType();

         if (configurator.GetEntityConfiguration(entityType) is null)
         {
            continue;
         }
         
         var auditedEntity = new AuditedEntity
         {
            Entry = entry,
            ActionType = entry.State.ToAuditActionType(),
            Name = entityType.Name,

            PrimaryKeyValue = GetPrimaryKeyValue(entry),
            Type = entityType
         };

         foreach (var property in entry.Properties)
         {
            auditedEntity.PropertyOriginalValues[property.Metadata.Name] = property.OriginalValue;
            auditedEntity.PropertyCurrentValues[property.Metadata.Name] = property.CurrentValue;
         }

         _entities.Add(auditedEntity);
      }
   }

   internal void UpdateTrackedData()
   {
      foreach (var entity in _entities)
      {
         entity.PrimaryKeyValue = GetPrimaryKeyValue(entity.Entry);

         foreach (var property in entity.Entry.Properties)
         {
            entity.PropertyCurrentValues[property.Metadata.Name] = property.CurrentValue;
         }
      }
   }

   internal async Task PublishAuditTrailEventData()
   {
      var auditTrailEventData = ProcessTrackedData();

      if (auditTrailEventData.Entities.Count == 0)
      {
         return;
      }

      await consumer.ConsumeAuditTrailAsync(auditTrailEventData);
      _entities.Clear();
   }

   private AuditTrailEventData ProcessTrackedData()
   {
      var transformedEntities = new List<AuditTrailEventEntity>();

      foreach (var entity in _entities)
      {
         if (!AuditTrailHelper.ShouldProcessEntity(
                entity.ActionType,
                entity.Type,
                configurator,
                out var entityConfig))
         {
            continue;
         }

         RemoveUnchangedProperties(entity);

         var transformedProps = AuditTrailHelper.TransformProperties(
            entity.PropertyCurrentValues,
            entityConfig!.Properties
         );

         var eventEntity = new AuditTrailEventEntity(
            entity.Entry,
            entityConfig.ServiceName,
            entity.ActionType,
            entity.Name,
            entityConfig.PermissionToRead,
            entity.PrimaryKeyValue,
            transformedProps
         );

         transformedEntities.Add(eventEntity);
      }

      return new AuditTrailEventData(transformedEntities);
   }

   private static void RemoveUnchangedProperties(AuditedEntity entity)
   {
      if (entity.ActionType is AuditActionType.Create)
      {
         return;
      }

      var propertiesToRemove = entity.PropertyCurrentValues
                                     .Where(kv =>
                                        entity.PropertyOriginalValues.TryGetValue(kv.Key, out var originalValue) &&
                                        Equals(originalValue, kv.Value))
                                     .Select(kv => kv.Key)
                                     .ToList();

      foreach (var property in propertiesToRemove)
      {
         entity.PropertyOriginalValues.Remove(property);
         entity.PropertyCurrentValues.Remove(property);
      }
   }


   private static string GetPrimaryKeyValue(EntityEntry entry)
   {
      return string.Join("_",
         entry.Properties
              .Where(p => p.Metadata.IsPrimaryKey())
              .Select(p => p.CurrentValue?.ToString() ?? string.Empty));
   }
}