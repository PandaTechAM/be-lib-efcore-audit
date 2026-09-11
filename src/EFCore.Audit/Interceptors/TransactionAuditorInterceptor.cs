using System.Data.Common;
using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>Publishes audit trail events when a database transaction commits and drops them when it rolls back.</summary>
public class TransactionAuditorInterceptor : DbTransactionInterceptor
{
    private readonly AuditTrailTrackingService _trackingService;

    /// <summary>Creates the interceptor for the given tracking service.</summary>
    public TransactionAuditorInterceptor(
        AuditTrailTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    /// <inheritdoc />
    public override async Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await _trackingService.CommitTransactionAsync(
                eventData.Context,
                eventData.TransactionId,
                cancellationToken);
        }

        await base.TransactionCommittedAsync(
            transaction,
            eventData,
            cancellationToken);
    }

    /// <inheritdoc />
    public override void TransactionCommitted(
        DbTransaction transaction,
        TransactionEndEventData eventData)
    {
        if (eventData.Context is not null)
        {
            _trackingService.CommitTransactionAsync(
                    eventData.Context,
                    eventData.TransactionId,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        base.TransactionCommitted(
            transaction,
            eventData);
    }

    /// <inheritdoc />
    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RollbackTransaction(eventData);

        return base.TransactionRolledBackAsync(
            transaction,
            eventData,
            cancellationToken);
    }

    /// <inheritdoc />
    public override void TransactionRolledBack(
        DbTransaction transaction,
        TransactionEndEventData eventData)
    {
        RollbackTransaction(eventData);

        base.TransactionRolledBack(
            transaction,
            eventData);
    }

    /// <inheritdoc />
    public override Task TransactionFailedAsync(
        DbTransaction transaction,
        TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RollbackTransaction(eventData);

        return base.TransactionFailedAsync(
            transaction,
            eventData,
            cancellationToken);
    }

    /// <inheritdoc />
    public override void TransactionFailed(
        DbTransaction transaction,
        TransactionErrorEventData eventData)
    {
        RollbackTransaction(eventData);

        base.TransactionFailed(
            transaction,
            eventData);
    }

    private void RollbackTransaction(TransactionEventData eventData)
    {
        if (eventData.Context is not null)
        {
            _trackingService.RollbackTransaction(
                eventData.Context,
                eventData.TransactionId);
        }
    }
}
