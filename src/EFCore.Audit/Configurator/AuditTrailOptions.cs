using System.Linq.Expressions;
using EFCore.Audit.Helpers;
using EFCore.Audit.Models;

namespace EFCore.Audit.Configurator;

/// <summary>Registration-wide audit trail settings.</summary>
public sealed class AuditTrailOptions
{
    internal List<(Type BaseType, SoftDeleteMarker Marker)> SoftDeleteMarkers { get; } = [];

    /// <summary>
    ///     Audits an update that sets <paramref name="property" /> to <paramref name="deletedValue" /> as a delete for every
    ///     audited entity deriving from <typeparamref name="TBase" /> (for example a shared soft-delete base class). An
    ///     entity configurator's own marker takes precedence; when several base types match, the first registered wins.
    /// </summary>
    public AuditTrailOptions SetSoftDeleteMarker<TBase, TProperty>(
        Expression<Func<TBase, TProperty>> property,
        TProperty deletedValue)
        where TBase : class
    {
        SoftDeleteMarkers.Add((typeof(TBase),
            new SoftDeleteMarker(AuditTrailHelper.GetPropertyName(property), deletedValue)));
        return this;
    }
}
