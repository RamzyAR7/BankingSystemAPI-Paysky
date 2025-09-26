using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Moq;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.Identity;

namespace BankingSystemAPI.UnitTests
{
    public class TransactionAuthorizationServiceTests
    {
        [Fact]
        public async Task CanInitiateTransferAsync_Allows_SuperAdmin()
        {
            var currentUserMock = new Mock<ICurrentUserService>();
            var uowMock = new Mock<IUnitOfWork>();
            var scopeResolverMock = new Mock<IScopeResolver>();
            var accountRepoMock = new Mock<IAccountRepository>();

            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "SuperAdmin" });
            scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(BankingSystemAPI.Domain.Constant.AccessScope.Global);

            // Use concrete CheckingAccount for test
            var sourceAccount = new CheckingAccount { Id = 1, UserId = "user1", User = new ApplicationUser { Id = "user1" } };
            var targetAccount = new CheckingAccount { Id = 2, UserId = "user2", User = new ApplicationUser { Id = "user2" } };
            accountRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<System.Func<Account, bool>>>(), It.IsAny<Expression<System.Func<Account, object>>[]>(), It.IsAny<bool>()))
                .ReturnsAsync((Expression<System.Func<Account, bool>> predicate, Expression<System.Func<Account, object>>[] includes, bool asNoTracking) =>
                {
                    if (predicate.Compile().Invoke(sourceAccount)) return sourceAccount;
                    if (predicate.Compile().Invoke(targetAccount)) return targetAccount;
                    return null;
                });
            uowMock.Setup(u => u.AccountRepository).Returns(accountRepoMock.Object);

            var service = new TransactionAuthorizationService(currentUserMock.Object, uowMock.Object, scopeResolverMock.Object);
            await service.CanInitiateTransferAsync(1, 2); // Should not throw
        }
    }
}
