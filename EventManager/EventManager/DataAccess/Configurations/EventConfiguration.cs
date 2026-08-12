using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.DataAccess.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("events");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(e => e.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            builder.Property(b => b.StartAt)
                .HasColumnName("start_at")
                .IsRequired();

            builder.Property(b => b.EndAt)
                .HasColumnName("end_at")
                .IsRequired();

            builder.Property(b => b.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            builder.Property(b => b.AvailableSeats)
                .HasColumnName("available_seats")
                .IsRequired();

            builder.HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
