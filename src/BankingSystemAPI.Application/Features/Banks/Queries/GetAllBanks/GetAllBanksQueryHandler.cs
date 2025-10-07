#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Queries.GetAllBanks
{
    public class GetAllBanksQueryHandler : IQueryHandler<GetAllBanksQuery, List<BankSimpleResDto>>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllBanksQueryHandler> _logger;

        public GetAllBanksQueryHandler(IUnitOfWork uow, IMapper mapper, ILogger<GetAllBanksQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<BankSimpleResDto>>> Handle(GetAllBanksQuery request, CancellationToken cancellationToken)
        {
            // Validate and normalize parameters using functional approach
            var parametersResult = ValidateAndNormalizeParameters(request);
            if (!parametersResult) // Using implicit bool operator!
                return Result<List<BankSimpleResDto>>.Failure(parametersResult.ErrorItems);

            var queryResult = await ExecuteQueryAsync(parametersResult.Value!);
            
            // Add side effects using ResultExtensions
            queryResult.OnSuccess(() => 
                {
                    _logger.LogDebug("Retrieved {Count} banks with pagination: Page={Page}, Size={Size}", 
                        queryResult.Value!.Count, parametersResult.Value!.PageNumber, parametersResult.Value.PageSize);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Failed to retrieve banks. Errors: {Errors}",
                        string.Join(", ", errors));
                });

            return queryResult;
        }

        private Result<QueryParameters> ValidateAndNormalizeParameters(GetAllBanksQuery request)
        {
            // Apply business rules for pagination parameters
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : Math.Min(request.PageSize, 100); // Cap at 100 for performance

            var parameters = new QueryParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Skip = (pageNumber - 1) * pageSize,
                OrderBy = request.OrderBy,
                OrderDirection = request.OrderDirection
            };

            return Result<QueryParameters>.Success(parameters);
        }

        private async Task<Result<List<BankSimpleResDto>>> ExecuteQueryAsync(QueryParameters parameters)
        {
            try
            {
                var spec = new PagedSpecification<Bank>(
                    parameters.Skip, 
                    parameters.PageSize, 
                    parameters.OrderBy, 
                    parameters.OrderDirection);

                var banks = await _uow.BankRepository.ListAsync(spec);
                var mapped = _mapper.Map<List<BankSimpleResDto>>(banks);

                return Result<List<BankSimpleResDto>>.Success(mapped);
            }
            catch (Exception ex)
            {
                return Result<List<BankSimpleResDto>>.BadRequest($"Failed to retrieve banks: {ex.Message}");
            }
        }

        private class QueryParameters
        {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int Skip { get; set; }
            public string? OrderBy { get; set; }
            public string? OrderDirection { get; set; }
        }
    }
}

