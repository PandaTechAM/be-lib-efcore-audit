using System.Text.Json;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace EFCore.Audit.Demo;

public class AuditTrailConsumer : IAuditTrailConsumer
{
   public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData, CancellationToken cancellationToken)
   {
      var log = AuditTrailEventDataDebug.DebugLog(auditTrailEventData);
      Console.WriteLine(log);
      return Task.CompletedTask;
   }
}

public record AuditTrailEventDataDebug(
   string? ServiceName,
   AuditActionType ActionType,
   string Name,
   object? ReadPermission,
   string PrimaryKeyValue,
   Dictionary<string, object?> TrackedProperty)
{
   public static string DebugLog(AuditTrailEventData eventData)
   {
      var data = new List<AuditTrailEventDataDebug>();
      foreach (var entity in eventData.Entities)
      {
         var transformedAudit = new AuditTrailEventDataDebug(entity.ServiceName,
            entity.ActionType,
            entity.Name,
            entity.ReadPermission,
            entity.PrimaryKeyValue,
            entity.TrackedProperty);

         data.Add(transformedAudit);
      }

      return JsonSerializer.Serialize(data);
   }
}