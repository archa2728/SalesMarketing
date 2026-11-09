using SalesMarketing.Models;

namespace SalesMarketing.Repositories.Interfaces
{
    public interface IMarketingTeamRepository
    {
        Task<List<Marketingteam>> GetAllAsync();

        Task<Marketingteam?> GetByIdAsync(long id);

        Task<bool> CodeExistsAsync(string teamCode);

        Task<bool> CodeExistsForOtherAsync(string teamCode, long id);

        Task AddAsync(Marketingteam team);

        Task UpdateAsync(Marketingteam team);

        Task DeleteAsync(Marketingteam team);

        Task<Marketingteam?> GetActiveTeamForSalesmanAsync(long salesmanId);
    }
}
