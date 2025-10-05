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
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Token);

            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(rt => rt.CreatedOn)
                   .IsRequired();

            builder.Property(rt => rt.ExpiresOn)
                   .IsRequired();

            builder.Property(rt => rt.AbsoluteExpiresOn)
                   .IsRequired();

            builder.Property(rt => rt.RevokedOn)
                   .IsRequired(false);

            builder.ToTable("RefreshToken");
        }
    }
}

