using System.Threading.Tasks;
using Xunit;
using Moq;
using AutoMapper;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.UnitTests.UnitTests.Infrastructure;

public class GenericRepositoryTests : TestBase
{
    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
    }

    [Fact]
    public async Task Add_Get_Update_Delete_Workflow()
    {
        // Arrange
        var bank = new Bank { Name = "RepoTestBank", IsActive = true, CreatedAt = System.DateTime.UtcNow };

        // Act - Add
        await UnitOfWork.BankRepository.AddAsync(bank);
        await UnitOfWork.SaveAsync();

        var saved = Context.Banks.FirstOrDefault(b => b.Name == "RepoTestBank");
        Assert.NotNull(saved);

        // Act - Update
        saved!.Name = "RepoTestBankUpdated";
        await UnitOfWork.BankRepository.UpdateAsync(saved);
        await UnitOfWork.SaveAsync();

        var updated = Context.Banks.FirstOrDefault(b => b.Name == "RepoTestBankUpdated");
        Assert.NotNull(updated);

        // Act - Delete
        await UnitOfWork.BankRepository.DeleteAsync(updated!);
        await UnitOfWork.SaveAsync();

        var deleted = Context.Banks.FirstOrDefault(b => b.Name == "RepoTestBankUpdated");
        Assert.Null(deleted);
    }
}
