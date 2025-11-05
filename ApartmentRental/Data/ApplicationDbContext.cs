using ApartmentRental.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRental.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Apartment> Apartments { get; set; }

        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {

                entity.HasMany(u => u.Apartments)
                      .WithOne(a => a.Lessor)
                      .HasForeignKey(a => a.LessorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            builder.Entity<Apartment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(a => a.Price)
                      .IsRequired()
                      .HasColumnType("decimal(18, 2)");

                entity.Property(a => a.City)
                      .IsRequired();

                entity.HasIndex(a => a.City)
                      .HasDatabaseName("IX_Apartments_City");

                entity.Property(a => a.Latitude)
                    .IsRequired(false);

                entity.Property(a => a.Longitude)
                    .IsRequired(false);
            });


            builder.Entity<Photo>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.ImageUrl)
                      .IsRequired();


                entity.HasOne(p => p.Apartment)
                      .WithMany(a => a.Photos)
                      .HasForeignKey(p => p.ApartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
