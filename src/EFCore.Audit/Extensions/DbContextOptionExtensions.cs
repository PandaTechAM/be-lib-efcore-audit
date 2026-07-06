using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

/// <summary>DbContext options extensions for wiring up audit trail interceptors.</summary>
public static class DbContextOptionExtensions
{
    /// <summary>Registers the save-changes and transaction audit interceptors on the options builder.</summary>
    public static DbContextOptionsBuilder AddAuditTrailInterceptors(this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider sp)
    {
        optionsBuilder.AddInterceptors(sp.GetRequiredService<SaveChangesAuditorInterceptor>());
        optionsBuilder.AddInterceptors(sp.GetRequiredService<TransactionAuditorInterceptor>());

        return optionsBuilder;
    }
}
