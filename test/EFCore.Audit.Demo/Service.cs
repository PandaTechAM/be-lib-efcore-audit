using EFCore.Audit.Demo.Context;
using EFCore.Audit.Demo.Entities;
using EFCore.Audit.Demo.Enums;
using EFCore.Audit.Models;
using EFCore.Audit.Services.Implementations;
using EFCore.Audit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Demo;

public class Service(PostgresContext dbContext, IAuditTrailPublisher auditPublisher)
{
   public async Task CreatePostAsync(CancellationToken cancellationToken = default)
   {
      var blog = new Blog
      {
         Title = "null",
         BlogType = BlogType.Personal,
         EncryptedKey = [0, 1, 2, 3]
      };

      var post = new Post
      {
         Title = "New Post",
         Content = "This is a new post",
         Blog = blog
      };

      await dbContext.Posts.AddAsync(post, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
   }

   public async Task UpdatePostTitleAsync(CancellationToken cancellationToken = default)
   {
      var existingPost = await dbContext.Posts.FirstOrDefaultAsync(cancellationToken: cancellationToken);

      if (existingPost == null)
      {
         throw new Exception("Post not found");
      }

      existingPost.Title = "postTitleChanged";
      await dbContext.SaveChangesAsync(cancellationToken);
   }

   public async Task DeletePostAsync(CancellationToken cancellationToken = default)
   {
      var existingPost = await dbContext.Posts.FirstOrDefaultAsync(cancellationToken: cancellationToken);

      if (existingPost == null)
      {
         throw new Exception("Post not found");
      }

      dbContext.Posts.Remove(existingPost);
      await dbContext.SaveChangesAsync(cancellationToken);
   }

   public async Task CreatePostTransaction(CancellationToken cancellationToken = default)
   {
      await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

      try
      {
         var blog = new Blog
         {
            Title = "null",
            BlogType = BlogType.Personal,
            EncryptedKey = [0, 1, 2, 3]
         };

         var post = new Post
         {
            Title = "New Post",
            Content = "This is a new post",
            Blog = blog
         };

         await dbContext.Posts.AddAsync(post, cancellationToken);
         await dbContext.SaveChangesAsync(cancellationToken);

         var anotherPost = new Post
         {
            Title = "Another Post",
            Content = "This is another post",
            Blog = blog
         };

         await dbContext.Posts.AddAsync(anotherPost, cancellationToken);

         await dbContext.SaveChangesAsync(cancellationToken);

         dbContext.Remove(post);
         await dbContext.SaveChangesAsync(cancellationToken);
         await transaction.CommitAsync(cancellationToken);
      }
      catch (Exception)
      {
         await transaction.RollbackAsync(CancellationToken.None);
         throw;
      }
   }

   public async Task FailTransactionAsync(CancellationToken cancellationToken = default)
   {
      await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

      try
      {
         var blog = new Blog
         {
            Title = "null",
            BlogType = BlogType.Personal,
            EncryptedKey = [0, 1, 2, 3]
         };

         var post = new Post
         {
            Title = "New Post",
            Content = "This is a new post",
            Blog = blog
         };

         await dbContext.Posts.AddAsync(post, cancellationToken);
         await dbContext.SaveChangesAsync(cancellationToken);

         var anotherPost = new Post
         {
            Title = "Another Post",
            Content = "This is another post",
            Blog = blog
         };

         await dbContext.Posts.AddAsync(anotherPost, cancellationToken);

         await dbContext.SaveChangesAsync(cancellationToken);
         throw new Exception("Transaction failed");
      }
      catch (Exception)
      {
         await transaction.RollbackAsync(CancellationToken.None);
         throw;
      }
   }

   public async Task CreatePublish()
   {
      var posts = new List<Post>();

      var blog = new Blog
      {
         Id = 2,
         Title = "null",
         CreatedAt = DateTime.UtcNow,
         BlogType = BlogType.Personal,
         EncryptedKey = [1, 2, 3]
      };

      for (var i = 0; i < 10; i++)
      {
         var post = new Post
         {
            Title = $"Post {i}",
            Content = $"This is post {i}",
            Blog = blog
         };

         posts.Add(post);
      }

      var auditEntries = new List<ManualAuditEntry>
      {
         new(
            typeof(Blog),
            AuditActionType.Create,
            [
               new AuditEntryDetail(
                  PrimaryKeyIds: [blog.Id.ToString()],

                  ChangedProperties: new Dictionary<string, object?>
                  {
                     [nameof(blog.Id)] = blog.Id,
                     [nameof(blog.Title)] = blog.Title,
                     [nameof(blog.CreatedAt)] = blog.CreatedAt,
                     [nameof(blog.BlogType)] = blog.BlogType,
                     [nameof(blog.EncryptedKey)] = blog.EncryptedKey
                  }
               )
            ]
         )
      };

      var postDetails = new List<AuditEntryDetail>();
      foreach (var p in posts)
      {
         postDetails.Add(
            new AuditEntryDetail(
               PrimaryKeyIds: [p.Id.ToString()],
               ChangedProperties: new Dictionary<string, object?>
               {
                  [nameof(p.Title)] = p.Title,
                  [nameof(p.Content)] = p.Content,
                  [nameof(p.BlogId)] = p.BlogId
               }
            )
         );
      }

      auditEntries.Add(
         new ManualAuditEntry(
            EntityType: typeof(Post),
            Action: AuditActionType.Create,
            ChangedItems: postDetails
         )
      );

      await auditPublisher.BulkAuditAsync(auditEntries, CancellationToken.None);
   }
}