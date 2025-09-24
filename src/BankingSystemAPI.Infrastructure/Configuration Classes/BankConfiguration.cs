using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Configuration_Classes
{
    public class BankConfiguration : IEntityTypeConfiguration<Bank>
    {
        public void Configure(EntityTypeBuilder<Bank> builder)
        {
            builder.ToTable("Banks");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(b => b.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(b => b.IsActive)
                   .HasDefaultValue(true)
                   .IsRequired();

            builder.HasMany(b => b.ApplicationUsers)
                   .WithOne(u => u.Bank)
                   .HasForeignKey(u => u.BankId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
