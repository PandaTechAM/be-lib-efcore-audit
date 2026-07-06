using EFCore.Audit.Models;

namespace EFCore.Audit.Configurator;

/// <summary>Fluent configuration for auditing a single entity property.</summary>
/// <param name="propertyConfig">Backing configuration mutated by the fluent calls.</param>
public class PropertyConfigurator<TEntity, TProperty>(PropertyAuditConfiguration propertyConfig)
{
    /// <summary>Excludes the property from the audit trail.</summary>
    public PropertyConfigurator<TEntity, TProperty> Ignore()
    {
        propertyConfig.Ignore = true;
        return this;
    }

    /// <summary>Renames the property in the audit trail.</summary>
    public PropertyConfigurator<TEntity, TProperty> Rename(string newName)
    {
        propertyConfig.Name = newName;
        return this;
    }

    /// <summary>Transforms the property value before it is published (e.g. masking or hashing).</summary>
    public PropertyConfigurator<TEntity, TProperty> Transform<TOutput>(Func<TProperty, TOutput> transformFunc)
    {
        propertyConfig.Transform = value => { return transformFunc((TProperty)value!); };

        return this;
    }
}
