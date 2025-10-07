using System.Threading.Tasks;
using Xunit;
using Moq;
using AutoMapper;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.UnitTests.UnitTests.Infrastructure;

public class UnitOfWorkTests : TestBase
{
    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        // No mapping behavior required for these tests
    }

    [Fact]
    public async Task BeginTransaction_AddEntity_Commit_Persists()
    {
        // Arrange
        var bank = new Bank { Name = "TxnBank", IsActive = true, CreatedAt = System.DateTime.UtcNow };

        // Act
        await UnitOfWork.BeginTransactionAsync();
        await UnitOfWork.BankRepository.AddAsync(bank);
        await UnitOfWork.CommitAsync();

        // Assert - entity should be persisted
        var saved = Context.Banks.FirstOrDefault(b => b.Name == "TxnBank");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task BeginTransaction_AddEntity_Rollback_NotPersisted()
    {
        // Arrange
        var bank = new Bank { Name = "RollbackBank", IsActive = true, CreatedAt = System.DateTime.UtcNow };

        // Act
        await UnitOfWork.BeginTransactionAsync();
        await UnitOfWork.BankRepository.AddAsync(bank);
        await UnitOfWork.RollbackAsync();

        // Assert - entity should not be persisted
        var saved = Context.Banks.FirstOrDefault(b => b.Name == "RollbackBank");
        Assert.Null(saved);
    }
}
