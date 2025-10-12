#region Usings
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Infrastructure.Configuration_Classes
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.UserName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(u => u.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(u => u.NationalId)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.DateOfBirth)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.RoleId)
                .IsRequired();

            builder.HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique(false);

            builder.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique(false);

            builder.HasIndex(u => new { u.NormalizedUserName, u.BankId })
                .HasDatabaseName("IX_AspNetUsers_NormalizedUserName_BankId")
                .IsUnique()
                .HasFilter("[NormalizedUserName] IS NOT NULL AND [BankId] IS NOT NULL");

            builder.HasIndex(u => new { u.NormalizedEmail, u.BankId })
                .HasDatabaseName("IX_AspNetUsers_NormalizedEmail_BankId")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL AND [BankId] IS NOT NULL");

            builder.HasIndex(u => new { u.NationalId, u.BankId }).IsUnique().HasFilter("[BankId] IS NOT NULL");
            builder.HasIndex(u => new { u.PhoneNumber, u.BankId }).IsUnique().HasFilter("[BankId] IS NOT NULL");
            builder.HasIndex(u => new { u.FullName, u.BankId }).IsUnique().HasFilter("[BankId] IS NOT NULL");

            builder.HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

