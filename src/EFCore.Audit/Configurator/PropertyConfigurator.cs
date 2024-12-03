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
         if (value is TProperty typedValue)
         {
            return transformFunc(typedValue);
         }

         throw new InvalidCastException($"Cannot cast property value to type {typeof(TProperty).Name}");
      };

      return this;
   }
}