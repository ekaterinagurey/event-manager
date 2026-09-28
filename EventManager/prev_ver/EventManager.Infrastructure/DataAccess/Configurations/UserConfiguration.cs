using EventManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Infrastructure.DataAccess.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(u => u.Login)
                .HasColumnName("login")
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(u => u.Login)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("PasswordHash")
                .IsRequired();

            builder.Property(u => u.Role)
                .HasColumnName("role")
                .IsRequired();

            builder.HasMany<Booking>()
               .WithOne()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
