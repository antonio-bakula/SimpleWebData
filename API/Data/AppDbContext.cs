using Microsoft.EntityFrameworkCore;
using SimpleWebData.Models;

namespace SimpleWebData.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<WebSite> WebSites { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<PhotoGallery> PhotoGalleries { get; set; } = null!;
        public DbSet<Photo> Photos { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Page> Pages { get; set; } = null!;
        public DbSet<PageText> PageTexts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kreiranje unique indexa za Code na razini cijelog WebSite-a
            modelBuilder.Entity<WebSite>().HasIndex(w => w.Code).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<PhotoGallery>().HasIndex(p => new { p.WebSiteId, p.Code }).IsUnique();
            modelBuilder.Entity<Facility>().HasIndex(f => new { f.WebSiteId, f.Code }).IsUnique();
            modelBuilder.Entity<Page>().HasIndex(p => new { p.WebSiteId, p.Code }).IsUnique();
            modelBuilder.Entity<PageText>().HasIndex(pt => new { pt.PageId, pt.Code }).IsUnique();

            // Kompozitni unique index za PhotoGalleryId + FileName kako si zatražio zbog SEO pristupa slici
            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.PhotoGalleryId, p.FileName })
                .IsUnique();

            // Sprečavanje grešaka s "multiple cascade paths" ukoliko se WebSite obriše dok ovi modeli postoje,
            // jer SQLite (EF Core na određenim providerima) to ne dopušta.
            modelBuilder.Entity<Facility>()
                .HasOne(f => f.WebSite)
                .WithMany(w => w.Facilities)
                .HasForeignKey(f => f.WebSiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Page>()
                .HasOne(p => p.WebSite)
                .WithMany(w => w.Pages)
                .HasForeignKey(p => p.WebSiteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PageText>()
                .HasOne(pt => pt.WebSite)
                .WithMany(w => w.PageTexts)
                .HasForeignKey(pt => pt.WebSiteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
