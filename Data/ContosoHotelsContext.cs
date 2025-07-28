using Microsoft.EntityFrameworkCore;
using ContosoHotels.Models;
using System;

namespace ContosoHotels.Data
{
    public class ContosoHotelsContext : DbContext
    {
        public ContosoHotelsContext(DbContextOptions<ContosoHotelsContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<RoomService> RoomServices { get; set; }
        public DbSet<HousekeepingRequest> HousekeepingRequests { get; set; }

        // AI Concierge entities
        public DbSet<ConversationHistory> ConversationHistories { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<AgentInteraction> AgentInteractions { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Email).IsRequired();
            });

            // Configure Room entity
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(e => e.RoomNumber).IsUnique();
                entity.Property(e => e.RoomNumber).IsRequired();
                entity.Property(e => e.RoomType).IsRequired();
                entity.Property(e => e.PricePerNight).HasColumnType("decimal(18,2)");
            });

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                
                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Check-in date must be before check-out date
                entity.HasCheckConstraint("CK_Booking_CheckOutAfterCheckIn", 
                    "[CheckOutDate] > [CheckInDate]");
            });

            // Configure RoomService entity
            modelBuilder.Entity<RoomService>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ItemName).IsRequired();
                
                entity.HasOne(d => d.Booking)
                    .WithMany()
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure HousekeepingRequest entity
            modelBuilder.Entity<HousekeepingRequest>(entity =>
            {
                entity.HasOne(d => d.Booking)
                    .WithMany()
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ConversationHistory entity
            modelBuilder.Entity<ConversationHistory>(entity =>
            {
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StartTime);
                entity.Property(e => e.SessionId).IsRequired();
            });

            // Configure ChatMessage entity
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.Timestamp);
                entity.Property(e => e.MessageText).IsRequired();
                
                entity.HasOne(d => d.Conversation)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure AgentInteraction entity
            modelBuilder.Entity<AgentInteraction>(entity =>
            {
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.AgentType);
                entity.HasIndex(e => e.Timestamp);
                entity.Property(e => e.AgentType).IsRequired();
                entity.Property(e => e.Action).IsRequired();
                
                entity.HasOne(d => d.Conversation)
                    .WithMany(p => p.AgentInteractions)
                    .HasForeignKey(d => d.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure MenuItem entity
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsAvailable);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Category).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // Seed data will be added in a separate seeding service
        }
    }
}
