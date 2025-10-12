#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers
{
    public sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IList<UserResDto>>
    {
        private readonly IUserAuthorizationService _userAuthorizationService;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllUsersQueryHandler>? _logger;

        public GetAllUsersQueryHandler(
            IUserAuthorizationService userAuthorizationService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllUsersQueryHandler>? logger = null)
        {
            _userAuthorizationService = userAuthorizationService;
            _uow = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<IList<UserResDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            // Use FilterUsersAsync which applies proper authorization logic
            var filterResult = await _userAuthorizationService.FilterUsersAsync(
                _uow.UserRepository.Table,
                request.PageNumber,
                request.PageSize,
                request.OrderBy,
                request.OrderDirection);

            if (filterResult.IsFailure)
            {
                var op = Result<IList<UserResDto>>.Failure(filterResult.ErrorItems);
                LogResult(op, "user", "get-all");
                return op;
            }

            var (users, totalCount) = filterResult.Value!;

            // Map the results to DTOs
            var userDtos = _mapper.Map<IList<UserResDto>>(users);
            var success = Result<IList<UserResDto>>.Success(userDtos);
            LogResult(success, "user", "get-all");
            return success;
        }

        private void LogResult<T>(Result<T> result, string category, string operation)
        {
            if (_logger == null)
                return;

            if (result.IsSuccess)
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, category, operation);
            else
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, category, operation, string.Join(", ", result.Errors ?? Enumerable.Empty<string>()));
        }
    }
}
