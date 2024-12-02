using EFCore.Audit.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Extensions;

internal static class AuditActionTypeExtensions
{
   public static AuditActionType ToAuditActionType(this EntityState entityState)
   {
      return entityState switch
      {
         EntityState.Added => AuditActionType.Create,
         EntityState.Modified => AuditActionType.Update,
         EntityState.Deleted => AuditActionType.Delete,
         _ => AuditActionType.Other
      };
   }
   public static bool IsInAuditScope(this AuditActionType actionType)
   {
      return actionType is not AuditActionType.Other;
   }
}