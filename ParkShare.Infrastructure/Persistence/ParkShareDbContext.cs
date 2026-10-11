using Microsoft.EntityFrameworkCore;
using ParkShare.Domain.Entities;
using ParkShare.Domain.Entities.JoinModels;
using RoleEnum = ParkShare.Common.Enums.Roles;

namespace ParkShare.Infrastructure.Persistence
{
    /// <summary>
    /// Entity Framework Core context for ParkShare.
    /// </summary>
    public class ParkShareDbContext : DbContext
    {
        /// <summary>
        /// Creates the database context.
        /// </summary>
        /// <param name="options">Context options.</param>
        public ParkShareDbContext(DbContextOptions<ParkShareDbContext> options)
            : base(options) { }

        /// <summary>
        /// Users.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Roles.
        /// </summary>
        public DbSet<Role> Roles => Set<Role>();

        /// <summary>
        /// User-role links.
        /// </summary>
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<ListingAmenity> ListingAmenities => Set<ListingAmenity>();
        public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Rating> Ratings => Set<Rating>();

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.NoAction;
            }

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Type).HasMaxLength(450);
                entity.HasOne(n => n.Booking).WithMany().HasForeignKey(n => n.BookingId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(n => new { n.UserId, n.BookingId, n.Type }).IsUnique()
                    .HasFilter("[BookingId] IS NOT NULL");
                entity.HasIndex(n => new { n.UserId, n.CreatedAt, n.Id });
                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .Property(r => r.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ListingAmenity>()
                .HasKey(la => new { la.ListingId, la.AmenityId });

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<ListingAmenity>()
                .HasOne(la => la.Listing)
                .WithMany(l => l.ListingAmenities)
                .HasForeignKey(la => la.ListingId);

            modelBuilder.Entity<ListingAmenity>()
                .HasOne(la => la.Amenity)
                .WithMany(a => a.ListingAmenities)
                .HasForeignKey(la => la.AmenityId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = (long)RoleEnum.Renter, Name = nameof(RoleEnum.Renter) },
                new Role { Id = (long)RoleEnum.Owner, Name = nameof(RoleEnum.Owner) },
                new Role { Id = (long)RoleEnum.Admin, Name = nameof(RoleEnum.Admin) });

            modelBuilder.Entity<Rating>()
                .HasIndex(r => r.BookingId)
                .IsUnique();

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.PricePerHour).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<ListingPhoto>()
                .HasOne(lp => lp.Listing)
                .WithMany(l => l.Photos)
                .HasForeignKey(lp => lp.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AvailabilityWindow>()
                .HasOne(aw => aw.Listing)
                .WithMany(l => l.AvailabilityWindows)
                .HasForeignKey(aw => aw.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListingAmenity>()
                .HasOne(la => la.Listing)
                .WithMany(l => l.ListingAmenities)
                .HasForeignKey(la => la.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListingAmenity>()
                .HasOne(la => la.Amenity)
                .WithMany(a => a.ListingAmenities)
                .HasForeignKey(la => la.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Booking)
                .WithMany(b => b.ChatMessages)
                .HasForeignKey(cm => cm.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Booking)
                .WithOne(b => b.Rating)
                .HasForeignKey<Rating>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
