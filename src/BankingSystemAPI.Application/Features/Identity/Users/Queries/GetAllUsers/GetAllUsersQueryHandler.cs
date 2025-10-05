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
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers
{
    public sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IList<UserResDto>>
    {
        private readonly IUserAuthorizationService _userAuthorizationService;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllUsersQueryHandler(
            IUserAuthorizationService userAuthorizationService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userAuthorizationService = userAuthorizationService;
            _uow = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<IList<UserResDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            if (request.PageNumber <= 0 || request.PageSize <= 0)
            {
                return Result<IList<UserResDto>>.Failure(new[] { ApiResponseMessages.Validation.PageNumberAndPageSizeGreaterThanZero });
            }

            try
            {
                // Use FilterUsersAsync which applies proper authorization logic
                var filterResult = await _userAuthorizationService.FilterUsersAsync(
                    _uow.UserRepository.Table,
                    request.PageNumber,
                    request.PageSize,
                    request.OrderBy,
                    request.OrderDirection);

                if (filterResult.IsFailure)
                    return Result<IList<UserResDto>>.Failure(filterResult.Errors);

                var (users, totalCount) = filterResult.Value!;

                // Map the results to DTOs
                var userDtos = _mapper.Map<IList<UserResDto>>(users);
                return Result<IList<UserResDto>>.Success(userDtos);
            }
            catch (Exception ex)
            {
                return Result<IList<UserResDto>>.Failure(new[] { ex.Message });
            }
        }
    }
}
