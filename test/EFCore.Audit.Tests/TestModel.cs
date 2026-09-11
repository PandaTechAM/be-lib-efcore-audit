using EFCore.Audit.Configurator;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Tests;

public enum BlogStatus
{
    Active = 0,
    Deleted = 1
}

public class Blog
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public BlogStatus Status { get; set; }
    public string? Secret { get; set; }
    public string? Password { get; set; }
    public byte[]? Payload { get; set; }
}

public abstract class SoftDeletable
{
    public bool Deleted { get; set; }
}

public class Post : SoftDeletable
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public required string Title { get; set; }
}

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class BlogRule : AuditTrailConfigurator<Blog>
{
    public BlogRule()
    {
        SetSoftDeleteMarker(x => x.Status, BlogStatus.Deleted);
        RuleFor(x => x.Secret).Transform(x => x is null ? null : "***");
        RuleFor(x => x.Password).Ignore();
    }
}

public class PostRule : AuditTrailConfigurator<Post>;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().HasIndex(x => x.Title).IsUnique();
        modelBuilder.Entity<Post>().HasOne<Blog>().WithMany().HasForeignKey(x => x.BlogId);
    }
}

public sealed class AuditSink
{
    private readonly List<AuditTrailEventData> _events = [];

    public IReadOnlyList<AuditTrailEventData> Events => _events;

    public List<AuditTrailEventEntity> Entities => _events.SelectMany(x => x.Entities).ToList();

    public void Add(AuditTrailEventData eventData)
    {
        _events.Add(eventData);
    }

    public void Clear()
    {
        _events.Clear();
    }
}

public sealed class SinkConsumer(AuditSink sink) : IAuditTrailConsumer
{
    public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData, CancellationToken ct = default)
    {
        sink.Add(auditTrailEventData);
        return Task.CompletedTask;
    }
}
