using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BankingSystemAPI.Infrastructure.Services;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.DTOs.User;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace BankingSystemAPI.UnitTests.UnitTests.Infrastructure
{
    public class UserServiceTests
    {
        [Fact]
        public async Task CreateUser_WhenDbUpdateExceptionContainsFullName_ReturnsFullNameConflict()
        {
            // Arrange
            var userReq = new UserReqDto
            {
                Username = "jdoe",
                Email = "jdoe@example.com",
                FullName = "John Doe",
                NationalId = "1234567890",
                PhoneNumber = "01234567890",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                BankId = 1,
                Password = "P@ssw0rd",
                PasswordConfirm = "P@ssw0rd"
            };

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);

            // Users IQueryable must be provided for pre-checks
            userManagerMock.SetupGet(x => x.Users).Returns(new List<ApplicationUser>().AsQueryable());

            // Make CreateAsync throw DbUpdateException with inner message containing 'FullName'
            var dbEx = new DbUpdateException("DB error", new Exception("IX_AspNetUsers_FullName_BankId unique index violation: FullName"));
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ThrowsAsync(dbEx);

            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStoreMock.Object, null, null, null, null);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<ApplicationUser>(It.IsAny<UserReqDto>()))
                .Returns<UserReqDto>(r => new ApplicationUser { UserName = r.Username, Email = r.Email, FullName = r.FullName, NationalId = r.NationalId, PhoneNumber = r.PhoneNumber });

            var logger = new NullLogger<UserService>();

            var svc = new UserService(userManagerMock.Object, roleManagerMock.Object, mapperMock.Object, logger);

            // Act
            var result = await svc.CreateUserAsync(userReq);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Full name", result.Errors.FirstOrDefault() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("username", "username")]
        [InlineData("email", "email")]
        [InlineData("nationalid", "national id")]
        [InlineData("phonenumber", "phone number")]
        public async Task CreateUser_WhenDbUpdateExceptionContainsField_ReturnsFieldConflict(string innerMarker, string expectedSubstring)
        {
            // Arrange
            var userReq = new UserReqDto
            {
                Username = "jdoe",
                Email = "jdoe@example.com",
                FullName = "John Doe",
                NationalId = "1234567890",
                PhoneNumber = "01234567890",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                BankId = 1,
                Password = "P@ssw0rd",
                PasswordConfirm = "P@ssw0rd"
            };

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);

            userManagerMock.SetupGet(x => x.Users).Returns(new System.Collections.Generic.List<ApplicationUser>().AsQueryable());

            var dbEx = new DbUpdateException("DB error", new Exception($"Unique index violation: {innerMarker}"));
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ThrowsAsync(dbEx);

            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStoreMock.Object, null, null, null, null);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<ApplicationUser>(It.IsAny<UserReqDto>()))
                .Returns<UserReqDto>(r => new ApplicationUser { UserName = r.Username, Email = r.Email, FullName = r.FullName, NationalId = r.NationalId, PhoneNumber = r.PhoneNumber });

            var logger = new NullLogger<UserService>();

            var svc = new UserService(userManagerMock.Object, roleManagerMock.Object, mapperMock.Object, logger);

            // Act
            var result = await svc.CreateUserAsync(userReq);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(expectedSubstring, result.Errors.FirstOrDefault() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
