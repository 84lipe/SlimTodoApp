using Microsoft.EntityFrameworkCore;
using SlimTodoApp.Api.Domain.Models;

namespace SlimTodoApp.Api.Data;

public sealed class TodoContext : DbContext
{
    public TodoContext(DbContextOptions<TodoContext> options) : base(options)
    {

    }
    
    public DbSet<Todo> Todos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>()
        .HasKey(x => x.Id)
        .HasName("PK_Todo");
        
        base.OnModelCreating(modelBuilder);
    }
}