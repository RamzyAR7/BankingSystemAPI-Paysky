namespace BankingSystemAPI.Application.Interfaces.Infrastructure
{
    public interface IDbCapabilities
    {
        bool SupportsEfCoreAsync { get; }
    }
}
