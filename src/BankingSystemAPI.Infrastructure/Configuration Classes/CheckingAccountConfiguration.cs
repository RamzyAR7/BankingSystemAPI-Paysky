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
    public class CheckingAccountConfiguration : IEntityTypeConfiguration<CheckingAccount>
    {
        public void Configure(EntityTypeBuilder<CheckingAccount> builder)
        {
            builder.UseTpcMappingStrategy();
            builder.Property(ca => ca.OverdraftLimit)
                .HasColumnType("decimal(18,2)");
        }
    }
}
