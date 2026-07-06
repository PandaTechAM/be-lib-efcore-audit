using System.Data.Common;
using EFCore.Audit.Services.Implementations;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>Publishes audit trail events when a database transaction commits.</summary>
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
            _trackingService.UpdateTrackedData(eventData.Context);

            await _trackingService.PublishAuditTrailEventDataAsync(
                eventData.Context,
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
            _trackingService.UpdateTrackedData(eventData.Context);

            _trackingService.PublishAuditTrailEventDataAsync(
                    eventData.Context,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        base.TransactionCommitted(
            transaction,
            eventData);
    }
}
