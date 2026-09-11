namespace EFCore.Audit.Models;

/// <summary>Per-DbContext accumulator of tracked entities waiting to be published.</summary>
public sealed class AuditTrackingState
{
    /// <summary>Entities tracked so far for the current operation.</summary>
    public List<AuditedEntity> Entities { get; } = [];

    /// <summary>Pool lease of the context the entities were captured on; a new lease starts empty.</summary>
    internal int Lease { get; private set; }

    /// <summary>Caller transaction the entities were captured in; null for saves outside a transaction.</summary>
    internal Guid? TransactionId { get; set; }

    /// <summary>Number of entities captured before the save in progress, so a failed save can be dropped.</summary>
    internal int SaveCheckpoint { get; set; }

    /// <summary>Whether a save has started and has not yet reported success.</summary>
    internal bool SaveInProgress { get; set; }

    internal void Reset(int lease)
    {
        Entities.Clear();
        Lease = lease;
        TransactionId = null;
        SaveCheckpoint = 0;
        SaveInProgress = false;
    }

    internal void DiscardSaveInProgress()
    {
        if (!SaveInProgress)
        {
            return;
        }

        Entities.RemoveRange(SaveCheckpoint, Entities.Count - SaveCheckpoint);
        SaveInProgress = false;
    }
}
