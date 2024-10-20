using Microsoft.EntityFrameworkCore;

namespace KafkaPostgresqlToDo.Models;

public class TodoDbContext : DbContext
{
    public DbSet<Todo> Todos { get; set; }
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>()
            .Property(t => t.Metadata)
            .HasColumnType("jsonb");
    }
}
