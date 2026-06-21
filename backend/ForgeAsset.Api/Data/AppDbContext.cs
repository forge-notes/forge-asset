using ForgeAsset.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ForgeAsset.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var asset = modelBuilder.Entity<Asset>();
        asset.HasKey(x => x.Id);
        asset.HasIndex(x => x.AssetCode).IsUnique();
        asset.HasQueryFilter(x => x.DeletedAt == null);
        asset.Property(x => x.AssetCode).HasMaxLength(32).IsRequired();
        asset.Property(x => x.Name).HasMaxLength(200).IsRequired();
        asset.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("normal");
        asset.Property(x => x.Category).HasMaxLength(100);
        asset.Property(x => x.Brand).HasMaxLength(100);
        asset.Property(x => x.Model).HasMaxLength(100);
        asset.Property(x => x.SerialNumber).HasMaxLength(200);
        asset.Property(x => x.Location).HasMaxLength(200);
        asset.Property(x => x.Owner).HasMaxLength(100);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Asset>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
