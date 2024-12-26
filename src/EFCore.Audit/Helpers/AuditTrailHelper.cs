using EFCore.Audit.Configurator;
using EFCore.Audit.Extensions;
using EFCore.Audit.Models;

namespace EFCore.Audit.Helpers;

internal static class AuditTrailHelper
{
   internal static bool ShouldProcessEntity(AuditActionType actionType,
      Type entityType,
      AuditTrailConfigurator configurator,
      out EntityAuditConfiguration? entityConfig)
   {
      entityConfig = configurator.GetEntityConfiguration(entityType);
      if (entityConfig is null)
      {
         return false;
      }

      if (!actionType.IsInAuditScope())
      {
         return false;
      }

      return entityConfig.AuditActions is null || entityConfig.AuditActions.Contains(actionType);
   }

   internal static Dictionary<string, object?> TransformProperties(
      IReadOnlyDictionary<string, object?> sourceProperties,
      IReadOnlyDictionary<string, PropertyAuditConfiguration> configProperties)
   {
      var transformed = new Dictionary<string, object?>();

      foreach (var (propertyName, propertyValue) in sourceProperties)
      {
         if (configProperties.TryGetValue(propertyName, out var propConfig))
         {
            if (propConfig.Ignore)
            {
               continue;
            }

            var finalName = propConfig.Name ?? propertyName;
            var finalValue = propConfig.Transform?.Invoke(propertyValue) ?? propertyValue;

            transformed[finalName] = finalValue;
         }
         else
         {
            transformed[propertyName] = propertyValue;
         }
      }

      return transformed;
   }
}