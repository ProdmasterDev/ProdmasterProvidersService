using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ProvidersDomain.Models;
using System;
using System.Collections.Generic;

namespace ProdmasterProvidersService.Database
{
    public class UserContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Standart> Standarts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Specification>()
                .HasMany(c => c.Products)
                .WithMany(s => s.Specifications)
                .UsingEntity<ProductSpecification>(
                   j => j
                    .HasOne(pt => pt.Product)
                    .WithMany(t => t.ProductSpecifications)
                    .HasForeignKey(pt => pt.ProductId),
                j => j
                    .HasOne(pt => pt.Specification)
                    .WithMany(p => p.ProductSpecifications)
                    .HasForeignKey(pt => pt.SpecificationId),
                j =>
                {
                    j.HasKey(t => new { t.ProductId, t.SpecificationId });
                    j.ToTable("ProductSpecifications");
                });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            FillUpdatedDate();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
        {
            FillUpdatedDate();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public void FillUpdatedDate()
        {
            var newEntities = ChangeTracker.Entries()
                .Where(
                    x => x.State == EntityState.Added &&
                         x.Entity != null &&
                         x.Entity is ITimeStampedModel
                )
                .Select(x => x.Entity as ITimeStampedModel);

            var modifiedEntities = ChangeTracker.Entries()
                .Where(
                    x => x.State == EntityState.Modified &&
                         x.Entity != null &&
                         x.Entity is ITimeStampedModel
                )
                .Select(x => x.Entity as ITimeStampedModel);

            foreach (var newEntity in newEntities)
            {
                if (newEntity != null)
                {
                    if (newEntity.CreatedAt == default)
                        newEntity.CreatedAt = DateTime.UtcNow;
                    if (newEntity.LastModified == default)
                        newEntity.LastModified = DateTime.UtcNow;
                }
            }

            foreach (var modifiedEntity in modifiedEntities)
            {
                if (modifiedEntity != null) modifiedEntity.LastModified = DateTime.UtcNow;
            }
        }
    }
}
