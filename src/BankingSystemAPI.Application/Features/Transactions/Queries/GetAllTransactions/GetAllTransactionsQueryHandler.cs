using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Specifications.TransactionSpecification;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions
{
    public class GetAllTransactionsQueryHandler : IQueryHandler<GetAllTransactionsQuery, IEnumerable<TransactionResDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ITransactionAuthorizationService _transactionAuth;

        public GetAllTransactionsQueryHandler(IUnitOfWork uow, IMapper mapper, ITransactionAuthorizationService transactionAuth)
        {
            _uow = uow;
            _mapper = mapper;
            _transactionAuth = transactionAuth;
        }

        public async Task<Result<IEnumerable<TransactionResDto>>> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
        {
            var query = _uow.TransactionRepository.QueryWithAccountTransactions();
            var filterResult = await _transactionAuth.FilterTransactionsAsync(query, request.PageNumber, request.PageSize);
            
            if (filterResult.IsFailure)
                return Result<IEnumerable<TransactionResDto>>.Failure(filterResult.Errors);
            
            var (items, total) = filterResult.Value!;
            var dtoItems = items.Select(i => _mapper.Map<TransactionResDto>(i)).ToList();
            return Result<IEnumerable<TransactionResDto>>.Success(dtoItems);
        }
    }
}
