using System.Threading.Tasks;
using Xunit;
using Moq;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Authorization;
// Use explicit namespace for ICurrentUserService
using ICurrentUserService = BankingSystemAPI.Application.Interfaces.Identity.ICurrentUserService;

namespace BankingSystemAPI.UnitTests
{
    public class UserAuthorizationServiceTests
    {
        [Fact]
        public async Task CanViewUserAsync_Allows_SuperAdmin()
        {
            var currentUserMock = new Mock<ICurrentUserService>();
            var uowMock = new Mock<IUnitOfWork>();
            var scopeResolverMock = new Mock<IScopeResolver>();
            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "SuperAdmin" });
            scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(BankingSystemAPI.Domain.Constant.AccessScope.Global);
            var service = new UserAuthorizationService(currentUserMock.Object, uowMock.Object, scopeResolverMock.Object);
            await service.CanViewUserAsync("user1"); // Should not throw
        }
    }
}
