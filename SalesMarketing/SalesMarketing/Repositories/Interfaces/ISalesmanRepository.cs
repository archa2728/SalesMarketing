
using SalesMarketing.Models;

namespace SalesMarketing.Repositories.Interfaces
{
    public interface ISalesmanRepository
    {
        Task<List<Salesman>> GetAllAsync();
        Task<(List<Salesman> Items, int TotalCount)> GetPagedAsync(
    string? search,
    short? status,
    int pageNumber,
    int pageSize);

        Task<Salesman?> GetByIdAsync(long id);

        Task<bool> CodeExistsAsync(string code);

        Task<bool> CodeExistsForOtherAsync(string code, long id);

        Task AddAsync(Salesman salesman);

        Task UpdateAsync(Salesman salesman);

        Task<bool> IsAssignedToTeamAsync(long salesmanId);

        Task DeleteAsync(Salesman salesman);
    }
}
