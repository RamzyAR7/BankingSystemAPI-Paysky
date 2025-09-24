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
    public class InterestLogConfiguration : IEntityTypeConfiguration<InterestLog>
    {
        public void Configure(EntityTypeBuilder<InterestLog> builder)
        {
            builder.ToTable("InterestLogs");

            builder.Property(il => il.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
