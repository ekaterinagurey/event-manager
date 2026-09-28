using EventManager.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EventManager.Events.Infrastructure.DataAccess.Configurations
{
    public class ProcessedBookingConfiguration: IEntityTypeConfiguration<ProcessedBooking>
    {
        public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
        {
            builder.ToTable("processed_bookings");
            builder.HasKey(e => e.BookingId);
            builder.Property(e => e.ProcessedAtUtc).IsRequired();
        }
    }
}
