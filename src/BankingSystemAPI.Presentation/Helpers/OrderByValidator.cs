using System;

namespace BankingSystemAPI.Presentation.Helpers
{
    public static class OrderByValidator
    {
        public static bool IsValid(string? orderBy, string[] allowedFields)
        {
            if (string.IsNullOrWhiteSpace(orderBy)) return true;
            return Array.Exists(allowedFields, f => string.Equals(f, orderBy, StringComparison.OrdinalIgnoreCase));
        }
    }
}
