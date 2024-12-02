using EFCore.Audit.Demo.Context;
using EFCore.Audit.Demo.Entities;
using EFCore.Audit.Demo.Enums;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Demo;

public class Service(PostgresContext dbContext)
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
}