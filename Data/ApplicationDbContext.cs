using Microsoft.EntityFrameworkCore;
using dotnet_gs2_2025.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace dotnet_gs2_2025.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração compatível com MySQL e Oracle
        modelBuilder.Entity<User>(entity =>
        {
            // Nome da tabela em minúsculas para MySQL, maiúsculas para Oracle
            var tableName = Database.IsMySql() ? "users" : "USERS";
            entity.ToTable(tableName);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            // Usar CURRENT_TIMESTAMP para ambos os bancos
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
