using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

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

         RemoveUnchangedProperties(entity);

         Dictionary<string, object?> transformedProperties = new();

         foreach (var (propertyName, propertyValue) in entity.PropertyCurrentValues)
         {
            if (entityConfig.Properties.TryGetValue(propertyName, out var propertyConfig))
            {
               if (propertyConfig.Ignore)
               {
                  continue;
               }

               var transformedPropertyName = propertyConfig.Name ?? propertyName;

               var transformedValue = propertyConfig.Transform?.Invoke(propertyValue) ?? propertyValue;

               transformedProperties.Add(transformedPropertyName, transformedValue);
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