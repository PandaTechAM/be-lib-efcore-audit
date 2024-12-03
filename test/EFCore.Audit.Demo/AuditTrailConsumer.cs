using System.Text.Json;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;

namespace EFCore.Audit.Demo;

public class AuditTrailConsumer : IAuditTrailConsumer
{
   public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData)
   {
      Console.WriteLine(JsonSerializer.Serialize(auditTrailEventData));
      return Task.CompletedTask;
   }
}