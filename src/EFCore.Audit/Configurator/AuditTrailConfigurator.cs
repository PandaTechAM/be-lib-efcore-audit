using System.Linq.Expressions;
using EFCore.Audit.Models;

namespace EFCore.Audit.Configurator;

/// <summary>Non-generic base for entity audit configurators, allowing them to be discovered and built uniformly.</summary>
public abstract class AbstractAuditTrailConfigurator
{
    /// <summary>Builds the entity audit configuration.</summary>
    public abstract EntityAuditConfiguration Build();
}

/// <summary>Base class for defining audit rules for a specific entity type. Derive and configure in the constructor.</summary>
public abstract class AuditTrailConfigurator<TEntity> : AbstractAuditTrailConfigurator where TEntity : class
{
    private readonly EntityAuditConfiguration _entityConfig = new();

    /// <inheritdoc />
    public override EntityAuditConfiguration Build()
    {
        return _entityConfig;
    }

    /// <summary>Starts configuring audit behavior for the given entity property.</summary>
    protected PropertyConfigurator<TEntity, TProperty> RuleFor<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);

        if (!_entityConfig.Properties.ContainsKey(propertyName))
        {
            _entityConfig.Properties[propertyName] = new PropertyAuditConfiguration();
        }

        return new PropertyConfigurator<TEntity, TProperty>(_entityConfig.Properties[propertyName]);
    }

    /// <summary>Sets the row-level read permission required to view audit entries for this entity.</summary>
    protected AuditTrailConfigurator<TEntity> SetReadPermission(object? permission)
    {
        _entityConfig.PermissionToRead = permission;
        return this;
    }

    /// <summary>Sets the service name attributed to audited changes of this entity.</summary>
    protected AuditTrailConfigurator<TEntity> SetServiceName(string serviceName)
    {
        _entityConfig.ServiceName = serviceName;
        return this;
    }

    /// <summary>Restricts auditing to the given action types; when unset, all actions are audited.</summary>
    protected AuditTrailConfigurator<TEntity> WriteAuditTrailOnEvents(params AuditActionType[] auditActions)
    {
        _entityConfig.AuditActions = auditActions;
        return this;
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Invalid property expression.");
    }
}

/// <summary>Registry of built entity audit configurations, keyed by entity type.</summary>
public class AuditTrailConfigurator
{
    private readonly Dictionary<Type, EntityAuditConfiguration> _configurations = new();

    /// <summary>Registers or replaces the audit configuration for an entity type.</summary>
    public void AddEntityConfiguration(Type entityType, EntityAuditConfiguration config)
    {
        _configurations[entityType] = config;
    }

    /// <summary>Returns the audit configuration for the entity type, or null if it is not audited.</summary>
    public EntityAuditConfiguration? GetEntityConfiguration(Type entityType)
    {
        return _configurations.GetValueOrDefault(entityType);
    }

    /// <summary>Returns all registered entity audit configurations.</summary>
    public IReadOnlyDictionary<Type, EntityAuditConfiguration> GetAllConfigurations()
    {
        return _configurations;
    }
}
