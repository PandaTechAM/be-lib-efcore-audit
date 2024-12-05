using EFCore.Audit.Models;

namespace EFCore.Audit.Configurator;

public class PropertyConfigurator<TEntity, TProperty>(PropertyAuditConfiguration propertyConfig)
{
   public PropertyConfigurator<TEntity, TProperty> Ignore()
   {
      propertyConfig.Ignore = true;
      return this;
   }

   public PropertyConfigurator<TEntity, TProperty> Rename(string newName)
   {
      propertyConfig.Name = newName;
      return this;
   }

   public PropertyConfigurator<TEntity, TProperty> Transform<TOutput>(Func<TProperty, TOutput> transformFunc)
   {
      propertyConfig.Transform = value =>
      {
         return transformFunc((TProperty)value!);
      };

      return this;
   }
}