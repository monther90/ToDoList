using Microsoft.EntityFrameworkCore;
using Task.Models;

namespace Task.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<TodoItem> TodoItem { get; set; }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=ToDoList;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;");
            }
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TodoItem>(e =>
            {
                e.ToTable("ToolItem", "dbo");

                e.HasKey(t => t.Id);

                e.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                e.Property(t => t.Description)
                    .HasMaxLength(4000);

                e.Property(t => t.DeadLine)
                    .IsRequired();

                e.Property(t => t.FileName)
                    .HasMaxLength(255);

                e.Property(t => t.StoredFileName)
                    .HasMaxLength(255);
            });
        }
    }
}