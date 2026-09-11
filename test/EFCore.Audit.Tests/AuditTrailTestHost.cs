using EFCore.Audit.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Tests;

/// <summary>
///     One in-memory SQLite database behind a pool of exactly one context, so consecutive scopes lease the same
///     context instance the way a busy pooled application does.
/// </summary>
public sealed class AuditTrailTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly SqliteConnection _connection;

    private AuditTrailTestHost(WebApplication app, SqliteConnection connection)
    {
        _app = app;
        _connection = connection;
    }

    public AuditSink Sink => _app.Services.GetRequiredService<AuditSink>();

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public static async Task<AuditTrailTestHost> StartAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<AuditSink>();
        builder.AddAuditTrail<SinkConsumer>(
            options => options.SetSoftDeleteMarker<SoftDeletable, bool>(x => x.Deleted, true),
            typeof(AuditTrailTestHost).Assembly);
        builder.Services.AddDbContextPool<TestDbContext>(
            (sp, options) => options.UseSqlite(connection).AddAuditTrailInterceptors(sp),
            1);

        var app = builder.Build();
        var host = new AuditTrailTestHost(app, connection);

        await host.UseDbAsync(db => db.Database.EnsureCreatedAsync());
        host.Sink.Clear();

        return host;
    }

    public async Task UseDbAsync(Func<TestDbContext, Task> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TestDbContext>());
    }

    public void UseDb(Action<TestDbContext> action)
    {
        using var scope = _app.Services.CreateScope();
        action(scope.ServiceProvider.GetRequiredService<TestDbContext>());
    }

    public async Task SeedAsync(params object[] entities)
    {
        await UseDbAsync(async db =>
        {
            db.AddRange(entities);
            await db.SaveChangesAsync();
        });

        Sink.Clear();
    }
}
