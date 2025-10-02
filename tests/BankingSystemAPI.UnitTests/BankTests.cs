using AutoMapper;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Moq;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Features.Banks.Commands.CreateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank;
using BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus;
using BankingSystemAPI.Application.Features.Banks.Queries.GetAllBanks;
using BankingSystemAPI.Application.Features.Banks.Queries.GetBankById;
using BankingSystemAPI.Application.Features.Banks.Queries.GetBankByName;
using System.Linq;

namespace BankingSystemAPI.UnitTests
{
    public class BankTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private readonly CreateBankCommandHandler _createHandler;
        private readonly UpdateBankCommandHandler _updateHandler;
        private readonly DeleteBankCommandHandler _deleteHandler;
        private readonly SetBankActiveStatusCommandHandler _setActiveHandler;
        private readonly GetAllBanksQueryHandler _getAllHandler;
        private readonly GetBankByIdQueryHandler _getByIdHandler;
        private readonly GetBankByNameQueryHandler _getByNameHandler;

        public BankTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<Domain.Entities.Bank>(It.IsAny<BankReqDto>()))
                .Returns((BankReqDto req) => new Domain.Entities.Bank { Name = req.Name, IsActive = req.IsActive, CreatedAt = DateTime.UtcNow });

            mapperMock.Setup(m => m.Map<BankResDto>(It.IsAny<Domain.Entities.Bank>()))
                .Returns((Domain.Entities.Bank b) => new BankResDto { Id = b.Id, Name = b.Name, IsActive = b.IsActive, CreatedAt = b.CreatedAt });

            mapperMock.Setup(m => m.Map<List<BankSimpleResDto>>(It.IsAny<List<Domain.Entities.Bank>>()))
                .Returns((List<Domain.Entities.Bank> list) => list.Select(b => new BankSimpleResDto { Id = b.Id, Name = b.Name, IsActive = b.IsActive, CreatedAt = b.CreatedAt }).ToList());

            mapperMock.Setup(m => m.Map(It.IsAny<BankEditDto>(), It.IsAny<Domain.Entities.Bank>()))
                .Returns((BankEditDto dto, Domain.Entities.Bank existing) => { existing.Name = dto.Name; return existing; });

            _mapper = mapperMock.Object;

            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Cache.MemoryCacheService(memoryCache);

            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _unitOfWork = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            _createHandler = new CreateBankCommandHandler(_unitOfWork, _mapper);
            _updateHandler = new UpdateBankCommandHandler(_unitOfWork, _mapper);
            _deleteHandler = new DeleteBankCommandHandler(_unitOfWork);
            _setActiveHandler = new SetBankActiveStatusCommandHandler(_unitOfWork);
            _getAllHandler = new GetAllBanksQueryHandler(_unitOfWork, _mapper);
            _getByIdHandler = new GetBankByIdQueryHandler(_unitOfWork, _mapper);
            _getByNameHandler = new GetBankByNameQueryHandler(_unitOfWork, _mapper);
        }

        [Fact]
        public async Task Create_ReturnsCreatedBank()
        {
            var req = new BankReqDto { Name = "TestBank" };
            var res = await _createHandler.Handle(new CreateBankCommand(req), CancellationToken.None);
            Assert.True(res.Succeeded);
            var created = res.Value!;
            Assert.Equal("TestBank", created.Name);
            Assert.True(created.Id > 0);
        }

        [Fact]
        public async Task Create_DuplicateName_Fails()
        {
            var req = new BankReqDto { Name = "DupBank" };
            var r1 = await _createHandler.Handle(new CreateBankCommand(req), CancellationToken.None);
            Assert.True(r1.Succeeded);
            var r2 = await _createHandler.Handle(new CreateBankCommand(req), CancellationToken.None);
            Assert.False(r2.Succeeded);
            Assert.Contains(r2.Errors, e => e.Contains("same name"));
        }

        [Fact]
        public async Task GetAll_ReturnsBanks()
        {
            await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "A" }), CancellationToken.None);
            await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "B" }), CancellationToken.None);

            var list = (await _getAllHandler.Handle(new GetAllBanksQuery(), CancellationToken.None)).Value!;
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetById_ReturnsBank()
        {
            var created = (await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "FindMe" }), CancellationToken.None)).Value!;
            var fetched = (await _getByIdHandler.Handle(new GetBankByIdQuery(created.Id), CancellationToken.None)).Value!;
            Assert.Equal(created.Name, fetched.Name);
        }

        [Fact]
        public async Task GetByName_ReturnsBank()
        {
            var created = (await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "ByName" }), CancellationToken.None)).Value!;
            var fetched = (await _getByNameHandler.Handle(new GetBankByNameQuery("ByName"), CancellationToken.None)).Value!;
            Assert.Equal(created.Name, fetched.Name);
        }

        [Fact]
        public async Task Update_ChangesName()
        {
            var created = (await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "Old" }), CancellationToken.None)).Value!;
            var updated = (await _updateHandler.Handle(new UpdateBankCommand(created.Id, new BankEditDto { Name = "New" }), CancellationToken.None)).Value!;
            Assert.Equal("New", updated.Name);
        }

        [Fact]
        public async Task Delete_RemovesBank()
        {
            var created = (await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "ToDelete" }), CancellationToken.None)).Value!;
            var del = await _deleteHandler.Handle(new DeleteBankCommand(created.Id), CancellationToken.None);
            Assert.True(del.Succeeded);
            var get = await _getByIdHandler.Handle(new GetBankByIdQuery(created.Id), CancellationToken.None);
            Assert.False(get.Succeeded);
        }

        [Fact]
        public async Task SetActive_TogglesStatus()
        {
            var created = (await _createHandler.Handle(new CreateBankCommand(new BankReqDto { Name = "ActiveBank" }), CancellationToken.None)).Value!;
            var res = await _setActiveHandler.Handle(new SetBankActiveStatusCommand(created.Id, false), CancellationToken.None);
            Assert.True(res.Succeeded);
            var fetched = (await _getByIdHandler.Handle(new GetBankByIdQuery(created.Id), CancellationToken.None)).Value!;
            Assert.False(fetched.IsActive);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
