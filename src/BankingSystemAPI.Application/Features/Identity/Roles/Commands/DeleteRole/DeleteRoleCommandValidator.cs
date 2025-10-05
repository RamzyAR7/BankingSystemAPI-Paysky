#region Usings
using BankingSystemAPI.Domain.Constant;
using FluentValidation;
using System.Text.RegularExpressions;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole
{
    /// <summary>
    /// Simple FluentValidation validator for DeleteRoleCommand
    /// Uses straightforward role name-based protection
    /// </summary>
    public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
    {
        // Role ID format regex pattern for GUID format
        private static readonly Regex RoleIdPattern = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Protected system role names - simple list
        private static readonly HashSet<string> ProtectedRoleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(UserRole.SuperAdmin),
            nameof(UserRole.Admin),
            nameof(UserRole.Client)
        };

        public DeleteRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotNull()
                .WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Role ID"))
                .WithErrorCode("ROLE_ID_NULL")

                .NotEmpty()
                .WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Role ID"))
                .WithErrorCode("ROLE_ID_EMPTY")

                .Length(36, 36)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthRangeFormat, "Role ID", 36, 36))
                .WithErrorCode("ROLE_ID_INVALID_LENGTH")

                .Must(BeValidGuidFormat)
                .WithMessage(ApiResponseMessages.Validation.InvalidIdFormat.Replace("{0}", "Role ID"))
                .WithErrorCode("ROLE_ID_INVALID_FORMAT")

                .Must(NotContainInvalidCharacters)
                .WithMessage(ApiResponseMessages.Validation.InvalidPhoneNumberFormat)
                .WithErrorCode("ROLE_ID_INVALID_CHARS")

                .Must(NotBeProtectedRole)
                .WithMessage(ApiResponseMessages.Validation.ProtectedRoleDeletionNotAllowed)
                .WithErrorCode("ROLE_ID_SYSTEM_ROLE");
        }

        /// <summary>
        /// Validate GUID format
        /// </summary>
        private static bool BeValidGuidFormat(string? roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return false;

            return Guid.TryParse(roleId, out _) && RoleIdPattern.IsMatch(roleId);
        }

        /// <summary>
        /// Simple check for protected roles based on common patterns
        /// </summary>
        private static bool NotBeProtectedRole(string? roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return true;

            // Check common system role ID patterns
            var systemRolePatterns = new[]
            {
                "00000000-0000-0000-0000-000000000001", // SuperAdmin
                "00000000-0000-0000-0000-000000000002", // Admin  
                "00000000-0000-0000-0000-000000000003", // Client
            };

            if (systemRolePatterns.Contains(roleId, StringComparer.OrdinalIgnoreCase))
                return false;

            // Check if role ID contains protected role names
            return !ProtectedRoleNames.Any(roleName =>
                roleId.Contains(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validate valid characters only
        /// </summary>
        private static bool NotContainInvalidCharacters(string? roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return true;

            return roleId.All(c => char.IsLetterOrDigit(c) || c == '-');
        }
    }
}
