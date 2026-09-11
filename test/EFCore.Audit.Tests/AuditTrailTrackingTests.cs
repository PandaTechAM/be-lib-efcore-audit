using EFCore.Audit.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Tests;

public class AuditTrailTrackingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_PublishesEveryPropertyWithRulesApplied()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            db.Blogs.Add(new Blog { Title = "a", Secret = "s", Password = "p" });
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Create, entity.ActionType);
        Assert.Equal(nameof(Blog), entity.Name);
        Assert.Equal("1", entity.PrimaryKeyValue);
        Assert.Equal("a", entity.TrackedProperty[nameof(Blog.Title)]);
        Assert.Equal("***", entity.TrackedProperty[nameof(Blog.Secret)]);
        Assert.False(entity.TrackedProperty.ContainsKey(nameof(Blog.Password)));
    }

    [Fact]
    public async Task Update_PublishesOnlyModifiedProperties()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a", Secret = "s", Payload = [1, 2, 3] });

        await host.UseDbAsync(async db =>
        {
            var blog = await db.Blogs.SingleAsync(Ct);
            blog.Title = "b";
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Update, entity.ActionType);
        Assert.Equal([nameof(Blog.Title)], entity.TrackedProperty.Keys);
        Assert.Equal("b", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task HardDelete_PublishesTheRowAsItStoodInTheDatabase()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a", Secret = "s", Password = "p" });

        await host.UseDbAsync(async db =>
        {
            var blog = await db.Blogs.SingleAsync(Ct);
            blog.Title = "never saved";
            db.Blogs.Remove(blog);
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Delete, entity.ActionType);
        Assert.Equal("1", entity.PrimaryKeyValue);
        Assert.Equal(1, entity.TrackedProperty[nameof(Blog.Id)]);
        Assert.Equal("a", entity.TrackedProperty[nameof(Blog.Title)]);
        Assert.Equal(BlogStatus.Active, entity.TrackedProperty[nameof(Blog.Status)]);
        Assert.Equal("***", entity.TrackedProperty[nameof(Blog.Secret)]);
        Assert.False(entity.TrackedProperty.ContainsKey(nameof(Blog.Password)));
    }

    [Fact]
    public async Task HardDelete_OfDependentKeepsItsForeignKey()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a" });
        await host.SeedAsync(new Post { BlogId = 1, Title = "p" });

        await host.UseDbAsync(async db =>
        {
            db.Posts.Remove(await db.Posts.SingleAsync(Ct));
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Delete, entity.ActionType);
        Assert.Equal(1, entity.TrackedProperty[nameof(Post.BlogId)]);
        Assert.Equal("p", entity.TrackedProperty[nameof(Post.Title)]);
    }

    [Fact]
    public async Task EntityMarker_SettingTheDeletedValue_PublishesDeleteWithTheWholeRow()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a", Secret = "s" });

        await host.UseDbAsync(async db =>
        {
            var blog = await db.Blogs.SingleAsync(Ct);
            blog.Status = BlogStatus.Deleted;
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Delete, entity.ActionType);
        Assert.Equal("a", entity.TrackedProperty[nameof(Blog.Title)]);
        Assert.Equal(BlogStatus.Deleted, entity.TrackedProperty[nameof(Blog.Status)]);
        Assert.Equal("***", entity.TrackedProperty[nameof(Blog.Secret)]);
        Assert.False(entity.TrackedProperty.ContainsKey(nameof(Blog.Password)));
    }

    [Fact]
    public async Task EntityMarker_UpdatingAnAlreadyDeletedRow_StaysUpdate()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a", Status = BlogStatus.Deleted });

        await host.UseDbAsync(async db =>
        {
            var blog = await db.Blogs.SingleAsync(Ct);
            blog.Title = "b";
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Update, entity.ActionType);
        Assert.Equal([nameof(Blog.Title)], entity.TrackedProperty.Keys);
    }

    [Fact]
    public async Task RegistrationWideMarker_AppliesToDerivedEntities()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a" });
        await host.SeedAsync(new Post { BlogId = 1, Title = "p" });

        await host.UseDbAsync(async db =>
        {
            var post = await db.Posts.SingleAsync(Ct);
            post.Deleted = true;
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal(AuditActionType.Delete, entity.ActionType);
        Assert.Equal("p", entity.TrackedProperty[nameof(Post.Title)]);
        Assert.Equal(1, entity.TrackedProperty[nameof(Post.BlogId)]);
        Assert.Equal(true, entity.TrackedProperty[nameof(Post.Deleted)]);
    }

    [Fact]
    public async Task UnauditedEntity_IsNeverPublished()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            db.Tags.Add(new Tag { Name = "t" });
            await db.SaveChangesAsync(Ct);
        });

        Assert.Empty(host.Sink.Events);
    }

    [Fact]
    public async Task MultiRowSaveOutsideTransaction_PublishesOnce()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            db.Blogs.AddRange(new Blog { Title = "a" }, new Blog { Title = "b" }, new Blog { Title = "c" });
            await db.SaveChangesAsync(Ct);
        });

        var eventData = Assert.Single(host.Sink.Events);
        Assert.Equal(3, eventData.Entities.Count);
    }

    [Fact]
    public async Task FailedSave_IsNotPublishedWithTheNextSave()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a" });

        await host.UseDbAsync(async db =>
        {
            db.Blogs.Add(new Blog { Title = "a" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));

            db.ChangeTracker.Clear();
            db.Blogs.Add(new Blog { Title = "b" });
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal("b", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task CommittedTransaction_PublishesEverySaveOnceAtCommit()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);

            db.Blogs.Add(new Blog { Title = "a" });
            await db.SaveChangesAsync(Ct);
            db.Blogs.Add(new Blog { Title = "b" });
            await db.SaveChangesAsync(Ct);

            Assert.Empty(host.Sink.Events);

            await transaction.CommitAsync(Ct);
        });

        var eventData = Assert.Single(host.Sink.Events);
        Assert.Equal(["a", "b"], eventData.Entities.Select(x => x.TrackedProperty[nameof(Blog.Title)]));
    }

    [Fact]
    public async Task FailedSaveInsideTransaction_IsNotPublishedOnCommit()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        await host.SeedAsync(new Blog { Title = "a" });

        await host.UseDbAsync(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);

            db.Blogs.Add(new Blog { Title = "x" });
            await db.SaveChangesAsync(Ct);

            var duplicate = new Blog { Title = "a" };
            db.Blogs.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
            db.Entry(duplicate).State = EntityState.Detached;

            await transaction.CommitAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal("x", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task RolledBackTransaction_IsNeverPublished()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
            {
                db.Blogs.Add(new Blog { Title = "a" });
                await db.SaveChangesAsync(Ct);
                await transaction.RollbackAsync(Ct);
            }

            db.ChangeTracker.Clear();
            db.Blogs.Add(new Blog { Title = "b" });
            await db.SaveChangesAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal("b", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task AbandonedTransaction_IsNeverPublished()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        await host.UseDbAsync(async db =>
        {
            await using (await db.Database.BeginTransactionAsync(Ct))
            {
                db.Blogs.Add(new Blog { Title = "a" });
                await db.SaveChangesAsync(Ct);
            }

            db.ChangeTracker.Clear();

            await using var transaction = await db.Database.BeginTransactionAsync(Ct);
            db.Blogs.Add(new Blog { Title = "b" });
            await db.SaveChangesAsync(Ct);
            await transaction.CommitAsync(Ct);
        });

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal("b", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task PooledContext_DoesNotCarryPendingEntitiesIntoTheNextLease()
    {
        await using var host = await AuditTrailTestHost.StartAsync();
        DbContextId firstLease = default;
        DbContextId secondLease = default;

        await host.UseDbAsync(async db =>
        {
            firstLease = db.ContextId;
            await db.Database.BeginTransactionAsync(Ct);
            db.Blogs.Add(new Blog { Title = "a" });
            await db.SaveChangesAsync(Ct);
        });

        await host.UseDbAsync(async db =>
        {
            secondLease = db.ContextId;
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);
            await transaction.CommitAsync(Ct);

            db.Blogs.Add(new Blog { Title = "b" });
            await db.SaveChangesAsync(Ct);
        });

        Assert.Equal(firstLease.InstanceId, secondLease.InstanceId);
        Assert.NotEqual(firstLease.Lease, secondLease.Lease);

        var entity = Assert.Single(host.Sink.Entities);
        Assert.Equal("b", entity.TrackedProperty[nameof(Blog.Title)]);
    }

    [Fact]
    public async Task SynchronousSaveAndCommit_Publish()
    {
        await using var host = await AuditTrailTestHost.StartAsync();

        host.UseDb(db =>
        {
            db.Blogs.Add(new Blog { Title = "a" });
            db.SaveChanges();

            using var transaction = db.Database.BeginTransaction();
            db.Blogs.Single().Status = BlogStatus.Deleted;
            db.SaveChanges();
            transaction.Commit();
        });

        Assert.Equal([AuditActionType.Create, AuditActionType.Delete],
            host.Sink.Entities.Select(x => x.ActionType));
    }
}
