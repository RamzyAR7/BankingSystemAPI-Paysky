using Microsoft.AspNetCore.Http;
using System.Linq;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Presentation.Services
{
    public class SuccessMessageProvider : ISuccessMessageProvider
    {
        public string GetSuccessMessage(string httpMethod, string controller, string action, IQueryCollection? query = null)
        {
            controller = controller?.ToLowerInvariant() ?? string.Empty;
            action = action?.ToLowerInvariant() ?? string.Empty;

            return httpMethod switch
            {
                "DELETE" => GetDeleteMessage(controller, action),
                "PUT" => GetUpdateMessage(controller, action, query),
                "PATCH" => GetUpdateMessage(controller, action, query),
                "POST" => GetProcessMessage(controller, action),
                _ => ApiResponseMessages.Generic.OperationCompleted
            };
        }

        #region Delete Messages
        private static string GetDeleteMessage(string controller, string action)
        {
            var type = ControllerTypeExtensions.Parse(controller);

            return type switch
            {
                ControllerType.User => ApiResponseMessages.Delete.User.Success,
                ControllerType.Bank => ApiResponseMessages.Delete.Bank.Success,
                ControllerType.Role => ApiResponseMessages.Delete.Role.Success,
                ControllerType.Account => ApiResponseMessages.Delete.Account.Success,
                ControllerType.Currency => ApiResponseMessages.Delete.Currency.Success,
                ControllerType.CheckingAccount => ApiResponseMessages.Delete.CheckingAccount.Success,
                ControllerType.SavingsAccount => ApiResponseMessages.Delete.SavingsAccount.Success,
                _ => ApiResponseMessages.Delete.Generic.Success
            };
        }
        #endregion

        #region Update Messages
        private static string GetUpdateMessage(string controller, string action, IQueryCollection? query)
        {
            if (action.Contains("password", StringComparison.OrdinalIgnoreCase) || action.Contains("changepassword", StringComparison.OrdinalIgnoreCase))
                return ApiResponseMessages.Update.Password.Success;

            if (action.Contains("active", StringComparison.OrdinalIgnoreCase) || action.Contains("status", StringComparison.OrdinalIgnoreCase))
                return GetStatusMessage(controller, query);

            var specific = GetSpecificUpdateMessage(controller, action);
            if (!string.IsNullOrEmpty(specific)) return specific;

            var type = ControllerTypeExtensions.Parse(controller);

            return type switch
            {
                ControllerType.User => ApiResponseMessages.Update.User.Success,
                ControllerType.Bank => ApiResponseMessages.Update.Bank.Success,
                ControllerType.Role => ApiResponseMessages.Update.Role.Success,
                ControllerType.Account => ApiResponseMessages.Update.Account.Success,
                ControllerType.Currency => ApiResponseMessages.Update.Currency.Success,
                ControllerType.CheckingAccount => ApiResponseMessages.Update.CheckingAccount.Success,
                ControllerType.SavingsAccount => ApiResponseMessages.Update.SavingsAccount.Success,
                _ => ApiResponseMessages.Update.Generic.Success
            };
        }
        private static string GetStatusMessage(string controller, IQueryCollection? query)
        {
            var isActiveParam = query?["isActive"].FirstOrDefault();
            var isActivating = string.Equals(isActiveParam, "true", StringComparison.OrdinalIgnoreCase);

            var type = ControllerTypeExtensions.Parse(controller);

            return type switch
            {
                ControllerType.User => isActivating ? ApiResponseMessages.Status.User.Activated : ApiResponseMessages.Status.User.Deactivated,
                ControllerType.Bank => isActivating ? ApiResponseMessages.Status.Bank.Activated : ApiResponseMessages.Status.Bank.Deactivated,
                ControllerType.Account => isActivating ? ApiResponseMessages.Status.Account.Activated : ApiResponseMessages.Status.Account.Deactivated,
                ControllerType.Currency => isActivating ? ApiResponseMessages.Status.Currency.Activated : ApiResponseMessages.Status.Currency.Deactivated,
                _ => isActivating ? ApiResponseMessages.Status.Generic.Activated : ApiResponseMessages.Status.Generic.Deactivated
            };
        }
        private static string GetSpecificUpdateMessage(string controller, string action)
        {
            // Normalize controller to enum once
            var ctrlType = ControllerTypeExtensions.Parse(controller);

            // Handle user-roles controller (role assignment endpoint)
            if (ctrlType == ControllerType.UserRoles || controller == "user-roles" || controller == "userroles")
                return ApiResponseMessages.RolePermissions.UpdateSuccess;

            // Role controller updates that represent assignments (kept for compatibility)
            if (ctrlType == ControllerType.Role || ctrlType == ControllerType.RoleClaims)
                return ApiResponseMessages.Update.Role.AssignmentSuccess;

            // No specific message for other controllers/actions in current codebase
            return string.Empty;
        }
        #endregion

        #region Process Messages - for POST actions
        private static string GetProcessMessage(string controller, string action)
        {
            var type = ControllerTypeExtensions.Parse(controller);

            if (type == ControllerType.Auth)
            {
                if (action.Contains("login", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Authentication.LoginSuccess;
                if (action.Contains("logout", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Authentication.LogoutSuccess;
                if (action.Contains("refresh", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Authentication.TokenRefreshed;
                if (action.Contains("revoke", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Authentication.TokenRevoked;
                return ApiResponseMessages.Authentication.OperationSuccess;
            }

            if (type == ControllerType.Transaction)
            {
                if (action.Contains("deposit", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Transaction.DepositSuccess;
                if (action.Contains("withdraw", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Transaction.WithdrawSuccess;
                if (action.Contains("transfer", StringComparison.OrdinalIgnoreCase)) return ApiResponseMessages.Transaction.TransferSuccess;
                return ApiResponseMessages.Transaction.ProcessedSuccess;
            }

            // Handle creates for common resource controllers
            if (type == ControllerType.User) return ApiResponseMessages.Create.User.Success;
            if (type == ControllerType.Account) return ApiResponseMessages.Create.Account.Success;
            if (type == ControllerType.Bank) return ApiResponseMessages.Create.Bank.Success;
            if (type == ControllerType.Currency) return ApiResponseMessages.Create.Currency.Success;

            if (type == ControllerType.RoleClaims) return ApiResponseMessages.RolePermissions.AssignmentSuccess;
            if (type == ControllerType.UserRoles) return ApiResponseMessages.RolePermissions.UpdateSuccess;

            return ApiResponseMessages.Processing.OperationSuccess;
        }
        #endregion
    }
}
