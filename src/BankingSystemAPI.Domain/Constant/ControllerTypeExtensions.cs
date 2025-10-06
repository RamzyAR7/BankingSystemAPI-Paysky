using System.Linq;

namespace BankingSystemAPI.Domain.Constant
{
    public static class ControllerTypeExtensions
    {
        public static ControllerType Parse(string? controller)
        {
            if (string.IsNullOrWhiteSpace(controller))
                return ControllerType.Unknown;

            // normalize: keep letters and digits only
            var key = new string(controller.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            return key switch
            {
                "auth" => ControllerType.Auth,
                "user" => ControllerType.User,
                "users" => ControllerType.User,
                "role" => ControllerType.Role,
                "roles" => ControllerType.Role,
                "roleclaims" => ControllerType.RoleClaims,
                "userroles" => ControllerType.UserRoles,
                "account" => ControllerType.Account,
                "accounts" => ControllerType.Account,
                "checkingaccount" => ControllerType.CheckingAccount,
                "checkingaccounts" => ControllerType.CheckingAccount,
                "savingsaccount" => ControllerType.SavingsAccount,
                "savingsaccounts" => ControllerType.SavingsAccount,
                "currency" => ControllerType.Currency,
                "currencies" => ControllerType.Currency,
                "transaction" => ControllerType.Transaction,
                "transactions" => ControllerType.Transaction,
                "accounttransactions" => ControllerType.Transaction,
                "bank" => ControllerType.Bank,
                "banks" => ControllerType.Bank,
                _ => ControllerType.Unknown
            };
        }
    }
}
