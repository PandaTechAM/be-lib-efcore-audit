using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Helpers;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Services.Implementations;

/// <summary>Collects change-tracked entities per DbContext and publishes audit trail events to the consumer.</summary>
public class AuditTrailTrackingService
{
    private readonly AuditTrailConfigurator _configurator;
    private readonly IServiceScopeFactory _scopeFactory;

    // Keyed by the context instance so the state is collected with it; a pooled instance is told apart per lease.
    private readonly ConditionalWeakTable<DbContext, AuditTrackingState> _states = new();

    /// <summary>Creates the tracking service for the given configuration and scope factory.</summary>
    public AuditTrailTrackingService(
        AuditTrailConfigurator configurator,
        IServiceScopeFactory scopeFactory)
    {
        _configurator = configurator;
        _scopeFactory = scopeFactory;
    }

    internal void BeginSave(DbContext dbContext)
    {
        // Enumerating entries runs DetectChanges, which can throw; do it before touching the state.
        var entries = dbContext.ChangeTracker
            .Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var state = GetState(dbContext);

        // A save that never reported success (it failed, was canceled, or another interceptor threw) is dropped.
        state.DiscardSaveInProgress();

        // Pending entities belong to one caller transaction. Anything captured outside this save's transaction was
        // left by a failed save or an abandoned transaction and must never be published.
        var transactionId = dbContext.Database.CurrentTransaction?.TransactionId;

        if (transactionId is null || transactionId != state.TransactionId)
        {
            state.Entities.Clear();
            state.TransactionId = transactionId;
        }

        state.SaveCheckpoint = state.Entities.Count;
        state.SaveInProgress = true;

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var entityConfig = _configurator.GetEntityConfiguration(entityType);

            if (entityConfig is null)
            {
                continue;
            }

            var auditedEntity = new AuditedEntity
            {
                Entry = entry,
                ActionType = entry.State.ToAuditActionType(),
                CapturedState = entry.State,
                Name = entityType.Name,
                PrimaryKeyValue = GetPrimaryKeyValue(entry),
                Type = entityType
            };

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;

                auditedEntity.PropertyOriginalValues[propertyName] = property.OriginalValue;
                auditedEntity.PropertyCurrentValues[propertyName] = property.CurrentValue;

                if (property.IsModified)
                {
                    auditedEntity.ModifiedProperties.Add(propertyName);
                }
            }

            if (auditedEntity.CapturedState == EntityState.Modified &&
                IsSoftDelete(auditedEntity, entityConfig.SoftDeleteMarker))
            {
                auditedEntity.ActionType = AuditActionType.Delete;
            }

            state.Entities.Add(auditedEntity);
        }
    }

    internal async Task EndSaveAsync(DbContext dbContext, CancellationToken ct)
    {
        if (!TryGetState(dbContext, out var state))
        {
            return;
        }

        state.SaveInProgress = false;
        RefreshCurrentValues(state);

        // Inside a caller transaction the entities wait for its commit.
        if (state.TransactionId is null)
        {
            await PublishAsync(state, ct);
        }
    }

    internal async Task CommitTransactionAsync(DbContext dbContext, Guid transactionId, CancellationToken ct)
    {
        // EF's own transaction around a save outside a caller transaction never matches; EndSaveAsync publishes those.
        if (!TryGetState(dbContext, out var state) || state.TransactionId != transactionId)
        {
            return;
        }

        // No save can be running at commit, so one still marked in progress failed and is dropped.
        state.DiscardSaveInProgress();
        RefreshCurrentValues(state);
        state.TransactionId = null;

        await PublishAsync(state, ct);
    }

    internal void RollbackTransaction(DbContext dbContext, Guid transactionId)
    {
        if (!TryGetState(dbContext, out var state) || state.TransactionId != transactionId)
        {
            return;
        }

        state.Entities.Clear();
        state.TransactionId = null;
        state.SaveInProgress = false;
    }

    private AuditTrackingState GetState(DbContext dbContext)
    {
        var state = _states.GetValue(dbContext, static _ => new AuditTrackingState());
        var lease = dbContext.ContextId.Lease;

        if (state.Lease != lease)
        {
            state.Reset(lease);
        }

        return state;
    }

    private bool TryGetState(DbContext dbContext, [NotNullWhen(true)] out AuditTrackingState? state)
    {
        if (_states.TryGetValue(dbContext, out state) && state.Lease == dbContext.ContextId.Lease)
        {
            return true;
        }

        state = null;
        return false;
    }

    private async Task PublishAsync(AuditTrackingState state, CancellationToken ct)
    {
        var auditTrailEventData = BuildEventData(state.Entities);
        state.Entities.Clear();

        if (auditTrailEventData.Entities.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();

        var consumer =
            scope.ServiceProvider.GetRequiredService<IAuditTrailConsumer>();

        await consumer.ConsumeAuditTrailAsync(
            auditTrailEventData,
            ct);
    }

    private AuditTrailEventData BuildEventData(List<AuditedEntity> entities)
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

            var transformedProps = AuditTrailHelper.TransformProperties(
                SelectValues(entity),
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

    private static Dictionary<string, object?> SelectValues(AuditedEntity entity)
    {
        return entity switch
        {
            { CapturedState: EntityState.Added } => entity.PropertyCurrentValues,

            // A removed row is audited as it stood in the database.
            { CapturedState: EntityState.Deleted } => entity.PropertyOriginalValues,

            // A soft delete is audited as the row now stands, deleted marker included.
            { ActionType: AuditActionType.Delete } => entity.PropertyCurrentValues,

            _ => entity.PropertyCurrentValues
                .Where(x => entity.ModifiedProperties.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value)
        };
    }

    private static bool IsSoftDelete(AuditedEntity entity, SoftDeleteMarker? marker)
    {
        return marker is not null &&
               entity.PropertyCurrentValues.TryGetValue(marker.PropertyName, out var current) &&
               Equals(current, marker.DeletedValue) &&
               !(entity.PropertyOriginalValues.TryGetValue(marker.PropertyName, out var original) &&
                 Equals(original, marker.DeletedValue));
    }

    private static void RefreshCurrentValues(AuditTrackingState state)
    {
        // Picks up store-generated keys and values once the save has run.
        foreach (var entity in state.Entities)
        {
            entity.PrimaryKeyValue = GetPrimaryKeyValue(entity.Entry);

            foreach (var property in entity.Entry.Properties)
            {
                entity.PropertyCurrentValues[property.Metadata.Name] = property.CurrentValue;
            }
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
