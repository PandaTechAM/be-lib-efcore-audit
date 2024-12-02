using EFCore.Audit.Models;

namespace EFCore.Audit.Configurator;

using System.Linq.Expressions;

public abstract class AbstractAuditTrailConfigurator
{
   public abstract EntityAuditConfiguration Build();
}

public abstract class AbstractAuditTrailConfigurator<TEntity> : AbstractAuditTrailConfigurator where TEntity : class
{
   private readonly EntityAuditConfiguration _entityConfig = new();

   public override EntityAuditConfiguration Build() => _entityConfig;

   protected PropertyConfigurator<TEntity, TProperty> RuleFor<TProperty>(
      Expression<Func<TEntity, TProperty>> propertyExpression)
   {
      var propertyName = GetPropertyName(propertyExpression);

      if (!_entityConfig.Properties.ContainsKey(propertyName))
         _entityConfig.Properties[propertyName] = new PropertyAuditConfiguration();

      return new PropertyConfigurator<TEntity, TProperty>(_entityConfig.Properties[propertyName]);
   }

   protected AbstractAuditTrailConfigurator<TEntity> SetReadPermission(object permission)
   {
      _entityConfig.PermissionToRead = permission;
      return this;
   }

   protected AbstractAuditTrailConfigurator<TEntity> SetServiceName(string serviceName)
   {
      _entityConfig.ServiceName = serviceName;
      return this;
   }

   protected AbstractAuditTrailConfigurator<TEntity> WriteAuditTrailOnEvents(params AuditActionType[] auditActions)
   {
      _entityConfig.AuditActions = auditActions;
      return this;
   }

   private static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
   {
      if (propertyExpression.Body is MemberExpression memberExpression)
         return memberExpression.Member.Name;

      throw new ArgumentException("Invalid property expression.");
   }
}

public class PropertyConfigurator<TEntity, TProperty>(PropertyAuditConfiguration propertyConfig)
{
   public PropertyConfigurator<TEntity, TProperty> Ignore()
   {
      propertyConfig.Ignore = true;
      return this;
   }

   public PropertyConfigurator<TEntity, TProperty> Transform<TOutput>(Func<TProperty, TOutput> transformFunc)
   {
      propertyConfig.Transform = value =>
      {
         if (value is TProperty typedValue)
         {
            return transformFunc(typedValue);
         }

         throw new InvalidCastException($"Cannot cast property value to type {typeof(TProperty).Name}");
      };

      return this;
   }
}

public class AuditTrailConfigurator
{
   private readonly Dictionary<Type, EntityAuditConfiguration> _configurations = new();

   public void AddEntityConfiguration(Type entityType, EntityAuditConfiguration config)
   {
      _configurations[entityType] = config;
   }

   public EntityAuditConfiguration? GetEntityConfiguration(Type entityType)
   {
      return _configurations.GetValueOrDefault(entityType);
   }

   public IReadOnlyDictionary<Type, EntityAuditConfiguration> GetAllConfigurations()
   {
      return _configurations;
   }
}