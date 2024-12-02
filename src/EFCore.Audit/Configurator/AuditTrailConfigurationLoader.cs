using System.Reflection;

namespace EFCore.Audit.Configurator;

public static class AuditTrailConfigurationLoader
{
   public static AuditTrailConfigurator LoadFromAssemblies(params Assembly[] assemblies)
   {
      var configurator = new AuditTrailConfigurator();

      foreach (var assembly in assemblies)
      {
         // Find all types inheriting from AbstractAuditTrailConfigurator<TEntity>
         var types = assembly.GetTypes()
                             .Where(t => t is { IsClass: true,
                                            IsAbstract: false,
                                            BaseType.IsGenericType: true
                                         } &&
                                         t.BaseType.GetGenericTypeDefinition() ==
                                         typeof(AbstractAuditTrailConfigurator<>));

         foreach (var type in types)
         {
            // Get the generic type argument (TEntity)
            var entityType = type.BaseType!.GetGenericArguments()[0];

            // Dynamically create an instance of the configurator
            var instance = Activator.CreateInstance(type);

            if (instance is not AbstractAuditTrailConfigurator configuratorInstance)
            {
               throw new InvalidOperationException(
                  $"The type '{type.FullName}' does not inherit from AbstractAuditTrailConfigurator<>");
            }

            // Build the configuration and add it to the configurator
            var entityConfig = configuratorInstance.Build();
            configurator.AddEntityConfiguration(entityType, entityConfig);
         }
      }

      return configurator;
   }
}