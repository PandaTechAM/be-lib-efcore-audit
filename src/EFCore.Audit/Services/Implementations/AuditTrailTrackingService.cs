using System.Collections.Concurrent;
using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Helpers;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Services.Implementations;

public class AuditTrailTrackingService
{
   private readonly AuditTrailConfigurator _configurator;
   private readonly IServiceScopeFactory _scopeFactory;

   private readonly ConcurrentDictionary<Guid, AuditTrackingState> _states = new();

   public AuditTrailTrackingService(
      AuditTrailConfigurator configurator,
      IServiceScopeFactory scopeFactory)
   {
      _configurator = configurator;
      _scopeFactory = scopeFactory;
   }

   internal void AddTrackedData(DbContext dbContext)
   {
      var state = _states.GetOrAdd(
         dbContext.ContextId.InstanceId,
         _ => new AuditTrackingState());

      foreach (var entry in dbContext.ChangeTracker.Entries())
      {
         var entityType = entry.Entity.GetType();

         if (_configurator.GetEntityConfiguration(entityType) is null)
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

         state.Entities.Add(auditedEntity);
      }
   }

   internal void UpdateTrackedData(DbContext dbContext)
   {
      if (!_states.TryGetValue(
             dbContext.ContextId.InstanceId,
             out var state))
      {
         return;
      }

      foreach (var entity in state.Entities)
      {
         entity.PrimaryKeyValue = GetPrimaryKeyValue(entity.Entry);

         foreach (var property in entity.Entry.Properties)
         {
            entity.PropertyCurrentValues[property.Metadata.Name] = property.CurrentValue;
         }
      }
   }

   internal async Task PublishAuditTrailEventDataAsync(
      DbContext dbContext,
      CancellationToken ct)
   {
      if (!_states.TryRemove(
             dbContext.ContextId.InstanceId,
             out var state))
      {
         return;
      }

      var auditTrailEventData =
         ProcessTrackedData(state.Entities);

      if (auditTrailEventData.Entities.Count == 0)
      {
         return;
      }

      using var scope = _scopeFactory.CreateScope();

      var consumer =
         scope.ServiceProvider.GetRequiredService<IAuditTrailConsumer>();

      await consumer.ConsumeAuditTrailAsync(
         auditTrailEventData,
         ct);
   }

   private AuditTrailEventData ProcessTrackedData(
      List<AuditedEntity> entities)
   {
      var transformedEntities = new List<AuditTrailEventEntity>();

      foreach (var entity in entities)
      {
         if (!AuditTrailHelper.ShouldProcessEntity(
                entity.ActionType,
                entity.Type,
                _configurator,
                out var entityConfig))
         {
            continue;
         }

         RemoveUnchangedProperties(entity);

         var transformedProps = AuditTrailHelper.TransformProperties(
            entity.PropertyCurrentValues,
            entityConfig!.Properties);

         transformedEntities.Add(
            new AuditTrailEventEntity(
               entity.Entry,
               entityConfig.ServiceName,
               entity.ActionType,
               entity.Name,
               entityConfig.PermissionToRead,
               entity.PrimaryKeyValue,
               transformedProps));
      }

      return new AuditTrailEventData(transformedEntities);
   }

   private static void RemoveUnchangedProperties(
      AuditedEntity entity)
   {
      if (entity.ActionType == AuditActionType.Create)
      {
         return;
      }

      var propertiesToRemove =
         entity.PropertyCurrentValues
               .Where(kv =>
                  entity.PropertyOriginalValues.TryGetValue(
                     kv.Key,
                     out var originalValue) &&
                  Equals(originalValue, kv.Value))
               .Select(x => x.Key)
               .ToList();

      foreach (var property in propertiesToRemove)
      {
         entity.PropertyOriginalValues.Remove(property);
         entity.PropertyCurrentValues.Remove(property);
      }
   }

   private static string GetPrimaryKeyValue(
      EntityEntry entry)
   {
      return string.Join("_",
         entry.Properties
              .Where(x => x.Metadata.IsPrimaryKey())
              .Select(x => x.CurrentValue?.ToString() ?? string.Empty));
   }
}