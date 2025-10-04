using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;

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

            var authResult = await _userAuthorizationService.CanViewUserAsync(request.UserId);

            if(!authResult)
            {
                return Result<UserResDto>.Failure(authResult.ErrorMessage);
            }
          

            // The UserService now returns Result<UserResDto> - will fail if user not found
            var userResult = await _userService.GetUserByIdAsync(request.UserId);
            
            if (!userResult.IsSuccess)
            {
                return Result<UserResDto>.Failure(userResult.Errors);
            }

            return Result<UserResDto>.Success(userResult.Value!);
        }
    }
}