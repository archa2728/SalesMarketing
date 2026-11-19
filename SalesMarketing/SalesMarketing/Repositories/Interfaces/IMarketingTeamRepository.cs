using SalesMarketing.Models;

namespace SalesMarketing.Repositories.Interfaces
{
    public interface IMarketingTeamRepository
    {
        Task<(List<Marketingteam> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            short? status);

        Task<Marketingteam?> GetByIdAsync(long id);

        Task<bool> CodeExistsAsync(string teamCode);

        Task<bool> CodeExistsForOtherAsync(string teamCode, long id);

        Task AddAsync(Marketingteam team);

        Task UpdateAsync(Marketingteam team);

        Task DeleteAsync(Marketingteam team);

        Task<Marketingteam?> GetActiveTeamForSalesmanAsync(long salesmanId);

        Task<Marketingteam?> GetByIdIncludingArchivedMembersAsync(long id);
    }
}