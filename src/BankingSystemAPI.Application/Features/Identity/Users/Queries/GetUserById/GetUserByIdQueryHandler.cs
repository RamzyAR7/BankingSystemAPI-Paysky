#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public GetUserByIdQueryHandler(
            IUserService userService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            if (_userAuthorizationService == null)
                return Result<UserResDto>.Failure(new ResultError(ErrorType.Forbidden, "Authorization service not available."));

            var authResult = await _userAuthorizationService.CanViewUserAsync(request.UserId);
            if (!authResult)
            {
                return Result<UserResDto>.Failure(new ResultError(ErrorType.Forbidden, authResult.ErrorMessage ?? "Forbidden"));
            }

            var userResult = await _userService.GetUserByIdAsync(request.UserId);
            if (!userResult.IsSuccess || userResult.Value == null)
            {
                return Result<UserResDto>.Failure(userResult.ErrorItems);
            }

            return Result<UserResDto>.Success(userResult.Value);
        }
    }
}
