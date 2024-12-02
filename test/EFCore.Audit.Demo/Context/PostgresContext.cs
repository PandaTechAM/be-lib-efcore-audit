using EFCore.Audit.Demo.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Demo.Context;

public class PostgresContext(DbContextOptions options) : DbContext(options)
{
   public DbSet<Blog> Blogs { get; set; }
   public DbSet<Post> Posts { get; set; }
}