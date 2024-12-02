using EFCore.Audit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

internal static class HttpAccessorExtensions
{
   public static IHttpContextAccessor GetHttpAccessor(this IServiceProvider sp)
   {
      return sp.GetRequiredService<IHttpContextAccessor>();
   }
   
   public static AuditTrailTrackingService? GetAuditTrailTrackingService(this IHttpContextAccessor contextAccessor)
   {
      return contextAccessor.HttpContext?.RequestServices.GetRequiredService<AuditTrailTrackingService>();
   }
}