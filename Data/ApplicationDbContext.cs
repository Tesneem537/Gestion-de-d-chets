using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WasteManagement3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WasteManagement3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Collector> Collector { get; set; }
        public DbSet<Hotel> Hotel { get; set; }
        public DbSet<Truck> Truck { get; set; }
        public DbSet<WasteCollection> WasteCollection { get; set; }
        public DbSet<GarageCheckin> GarageCheckin { get; set; }
        public DbSet<WasteType> WasteType { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<WeeklyStats> WeeklyStats { get; set; }

        // Configuring the connection string from appsettings.json or direct configuration
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())  // Get the base path of the app
                    .AddJsonFile("appsettings.json")  // Add appsettings.json for configuration
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);  // Set up the connection to the SQL Server
            }
        }

        // OnModelCreating for any extra configurations like foreign keys, constraints, etc.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<WeeklyStats>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HotelName).HasMaxLength(100);
                entity.Property(e => e.DayOfWeek).HasMaxLength(20);
                entity.Property(e => e.WasteType).HasMaxLength(50);
            });

            // Configure GarageCheckin relationships
            //modelBuilder.Entity<GarageCheckin>()
            //    .HasKey(g => g.CheckinID);

            //modelBuilder.Entity<GarageCheckin>()
            //    .HasOne(g => g.Collector)
            //    .WithMany()  // Assuming a collector can have many check-ins
            //    .HasForeignKey(g => g.CollectorID)
            //    .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<GarageCheckin>()
            //    .HasOne(g => g.Truck)
            //    .WithMany()  // Assuming a truck can have many check-ins
            //    .HasForeignKey(g => g.TruckID)
            //    .OnDelete(DeleteBehavior.Cascade);

            //// Configure WasteCollection relationships
            //modelBuilder.Entity<WasteCollection>()
            //    .HasOne(w => w.Collector)
            //    .WithMany(c => c.WasteCollection)  // Assuming collector can have many waste collections
            //    .HasForeignKey(w => w.CollectorID)
            //    .OnDelete(DeleteBehavior.Restrict);  // Prevent deleting collector if related waste collection exists

            //modelBuilder.Entity<WasteCollection>()
            //    .HasOne(wc => wc.WasteType)
            //    .WithMany()  // Assuming WasteType doesn't have any navigation property
            //    .HasForeignKey(wc => wc.WasteTypeID);

            //modelBuilder.Entity<WasteCollection>()
            //    .HasOne(w => w.Hotel)
            //    .WithMany(h => h.WasteCollection)  // Assuming hotel can have many waste collections
            //    .HasForeignKey(w => w.HotelID)
            //    .OnDelete(DeleteBehavior.Restrict);


            //modelBuilder.Entity<WasteCollection>()
            //    .HasOne(w => w.Truck)
            //    .WithMany(t => t.WasteCollection)  // Assuming truck can have many waste collections
            //    .HasForeignKey(w => w.TruckID)
            //    .OnDelete(DeleteBehavior.Restrict);  // Prevent deleting truck if related waste collection exists

            //// Configure relationships for Hotel
            //modelBuilder.Entity<Hotel>()
            //    .HasKey(h => h.HotelID);  // Assuming HotelID is the primary key

            //modelBuilder.Entity<Truck>()
            //    .HasKey(t => t.TruckID);  // Assuming TruckID is the primary key

            //modelBuilder.Entity<Users>()
            //    .HasKey(u => u.UserID);
            //modelBuilder.Entity<Collector>().ToTable("Collector");
            //modelBuilder.Entity<WasteCollection>().ToTable("WasteCollection");
        }

    }
}
