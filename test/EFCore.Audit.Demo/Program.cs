using System.Text.Json;
using EFCore.Audit.Demo;
using EFCore.Audit.Demo.Context;
using EFCore.Audit.Demo.Extensions;
using EFCore.Audit.Extensions;
using EFCore.Audit.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddAuditTrail(typeof(Program).Assembly);

builder.AddPostgresContext<PostgresContext>(
   "Server=localhost;Port=5432;Database=audit_test;User Id=test;Password=test;Pooling=true;");


builder.Services.AddOpenApi();
builder.Services.AddScoped<Service>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.EnsureCleanDb<PostgresContext>();
app.AddDemoEndpoints();

AuditTrailSubscriber.ConfigureAuditTrailHandler(auditTrailEventData =>
{
   Console.WriteLine("Subscribed to the event");
   Console.WriteLine(JsonSerializer.Serialize(auditTrailEventData));
});

app.Run();