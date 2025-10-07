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
                ControllerType.User => string.Format(ApiResponseMessages.Generic.DeletedFormat, "User"),
                ControllerType.Bank => string.Format(ApiResponseMessages.Generic.RemovedFormat, "Bank"),
                ControllerType.Role => string.Format(ApiResponseMessages.Generic.DeletedFormat, "Role"),
                ControllerType.Account => string.Format(ApiResponseMessages.Generic.RemovedFormat, "Account"),
                ControllerType.Currency => string.Format(ApiResponseMessages.Generic.RemovedFormat, "Currency"),
                ControllerType.CheckingAccount => string.Format(ApiResponseMessages.Generic.DeletedFormat, "Checking account"),
                ControllerType.SavingsAccount => string.Format(ApiResponseMessages.Generic.DeletedFormat, "Savings account"),
                _ => string.Format(ApiResponseMessages.Generic.DeletedFormat, "Resource")
            };
        }
        #endregion

        #region Update Messages
        private static string GetUpdateMessage(string controller, string action, IQueryCollection? query)
        {
            if (action.Contains("password", StringComparison.OrdinalIgnoreCase) || action.Contains("changepassword", StringComparison.OrdinalIgnoreCase))
                return string.Format(ApiResponseMessages.Generic.ChangedFormat, "Password");

            if (action.Contains("active", StringComparison.OrdinalIgnoreCase) || action.Contains("status", StringComparison.OrdinalIgnoreCase))
                return GetStatusMessage(controller, query);

            var specific = GetSpecificUpdateMessage(controller, action);
            if (!string.IsNullOrEmpty(specific)) return specific;

            var type = ControllerTypeExtensions.Parse(controller);

            return type switch
            {
                ControllerType.User => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "User"),
                ControllerType.Bank => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Bank"),
                ControllerType.Role => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Role"),
                ControllerType.Account => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Account"),
                ControllerType.Currency => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Currency"),
                ControllerType.CheckingAccount => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Checking account"),
                ControllerType.SavingsAccount => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Savings account"),
                _ => string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Resource")
            };
        }
        private static string GetStatusMessage(string controller, IQueryCollection? query)
        {
            var isActiveParam = query?["isActive"].FirstOrDefault();
            var isActivating = string.Equals(isActiveParam, "true", StringComparison.OrdinalIgnoreCase);

            var type = ControllerTypeExtensions.Parse(controller);

            return type switch
            {
                ControllerType.User => string.Format(isActivating ? ApiResponseMessages.Generic.ActivatedFormat : ApiResponseMessages.Generic.DeactivatedFormat, "User"),
                ControllerType.Bank => string.Format(isActivating ? ApiResponseMessages.Generic.ActivatedFormat : ApiResponseMessages.Generic.DeactivatedFormat, "Bank"),
                ControllerType.Account => string.Format(isActivating ? ApiResponseMessages.Generic.ActivatedFormat : ApiResponseMessages.Generic.DeactivatedFormat, "Account"),
                ControllerType.Currency => string.Format(isActivating ? ApiResponseMessages.Generic.ActivatedFormat : ApiResponseMessages.Generic.DeactivatedFormat, "Currency"),
                _ => string.Format(isActivating ? ApiResponseMessages.Generic.ActivatedFormat : ApiResponseMessages.Generic.DeactivatedFormat, "Resource")
            };
        }
        private static string GetSpecificUpdateMessage(string controller, string action)
        {
            // Normalize controller to enum once
            var ctrlType = ControllerTypeExtensions.Parse(controller);

            // Handle user-roles controller (role assignment endpoint)
            if (ctrlType == ControllerType.UserRoles || controller == "user-roles" || controller == "userroles")
                return string.Format(ApiResponseMessages.Generic.UpdatedFormat, "User role");

            // Role controller updates that represent assignments (kept for compatibility)
            if (ctrlType == ControllerType.Role || ctrlType == ControllerType.RoleClaims)
                return string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Role assignment");

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
                if (action.Contains("login", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Login");
                if (action.Contains("logout", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Logout");
                if (action.Contains("refresh", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Token refresh");
                if (action.Contains("revoke", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Token revoke");
                return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Authentication operation");
            }

            if (type == ControllerType.Transaction)
            {
                if (action.Contains("deposit", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.ProcessedFormat, "Deposit transaction");
                if (action.Contains("withdraw", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.ProcessedFormat, "Withdrawal transaction");
                if (action.Contains("transfer", StringComparison.OrdinalIgnoreCase)) return string.Format(ApiResponseMessages.Generic.CompletedFormat, "Transfer transaction");
                return string.Format(ApiResponseMessages.Generic.ProcessedFormat, "Transaction");
            }

            // Handle creates for common resource controllers
            if (type == ControllerType.User) return string.Format(ApiResponseMessages.Generic.CreatedFormat, "User");
            if (type == ControllerType.Account) return string.Format(ApiResponseMessages.Generic.CreatedFormat, "Account");
            if (type == ControllerType.Bank) return string.Format(ApiResponseMessages.Generic.CreatedFormat, "Bank");
            if (type == ControllerType.Currency) return string.Format(ApiResponseMessages.Generic.CreatedFormat, "Currency");

            if (type == ControllerType.RoleClaims) return string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Role assignment");
            if (type == ControllerType.UserRoles) return string.Format(ApiResponseMessages.Generic.UpdatedFormat, "User role");

            return string.Format(ApiResponseMessages.Generic.CreatedFormat, "Resource");
        }
        #endregion
    }
}
