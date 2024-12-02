using Microsoft.AspNetCore.Mvc;

namespace EFCore.Audit.Demo.Extensions;

public static class EndpointExtensions
{
   public static WebApplication AddDemoEndpoints(this WebApplication app)
   {
      app.MapGet("/CreatePostAsync", async ([FromServices] Service service) => await service.CreatePostAsync());
      app.MapGet("/UpdatePostTitleAsync",
         async ([FromServices] Service service) => await service.UpdatePostTitleAsync());
      app.MapGet("/DeletePostAsync", async ([FromServices] Service service) => await service.DeletePostAsync());
      app.MapGet("/FailTransactionAsync",
         async ([FromServices] Service service) => await service.FailTransactionAsync());
      app.MapGet("/CreatePostTransaction",
         async ([FromServices] Service service) => await service.CreatePostTransaction());

      return app;
   }
}