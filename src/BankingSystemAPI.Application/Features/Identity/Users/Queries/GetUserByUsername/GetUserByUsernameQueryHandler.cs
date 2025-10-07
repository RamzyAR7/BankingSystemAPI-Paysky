
#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserByUsername
{
    public sealed class GetUserByUsernameQueryHandler : IQueryHandler<GetUserByUsernameQuery, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public GetUserByUsernameQueryHandler(
            IUserService userService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(GetUserByUsernameQuery request, CancellationToken cancellationToken)
        {
            var result = await _userService.GetUserByUsernameAsync(request.Username);
            if (!result.IsSuccess || result.Value == null)
            {
                return Result<UserResDto>.Failure(result.ErrorItems);
            }

            var userDto = result.Value;

            if (_userAuthorizationService == null)
                return Result<UserResDto>.Failure(new ResultError(ErrorType.Forbidden, "Authorization service not available."));

            var authResult = await _userAuthorizationService.CanViewUserAsync(userDto.Id);
            if (!authResult)
            {
                return Result<UserResDto>.Failure(authResult.ErrorItems);
            }

            return Result<UserResDto>.Success(userDto);
        }
    }
}
