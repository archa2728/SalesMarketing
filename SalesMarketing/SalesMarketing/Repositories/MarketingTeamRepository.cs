using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;

namespace SalesMarketing.Repositories
{
    public class MarketingTeamRepository : IMarketingTeamRepository
    {
        private readonly SalesMarketingDbContext _context;

        public MarketingTeamRepository(SalesMarketingDbContext context)
        {
            _context = context;
        }

        // Get a filtered and paginated list of marketing teams.
        public async Task<(List<Marketingteam> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            short? status)
        {
            var query = _context.Marketingteams
                .Where(t => !t.Isarchived);

            // Search by team code or team name.
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchText = search.Trim();

                query = query.Where(t =>
                    EF.Functions.ILike(t.Teamcode, $"%{searchText}%") ||
                    EF.Functions.ILike(t.Teamname, $"%{searchText}%"));
            }

            // Filter by team status.
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            // Count matching records before pagination.
            int totalCount = await query.CountAsync();

            // Retrieve only the requested page.
            var teams = await query
                .OrderBy(t => t.Teamcode)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Marketingteamdetails
                    .Where(d => !d.Isarchived))
                .AsNoTracking()
                .ToListAsync();

            return (teams, totalCount);
        }

        // Get one team with its non-archived members.
        public async Task<Marketingteam?> GetByIdAsync(long id)
        {
            return await _context.Marketingteams
                .Include(t => t.Marketingteamdetails
                    .Where(d => !d.Isarchived))
                .ThenInclude(d => d.Salesman)
                .FirstOrDefaultAsync(t =>
                    t.Id == id && !t.Isarchived);
        }

        // Get one team with all members, including archived members.
        public async Task<Marketingteam?> GetByIdIncludingArchivedMembersAsync(
            long id)
        {
            return await _context.Marketingteams
                .Include(t => t.Marketingteamdetails)
                .FirstOrDefaultAsync(t =>
                    t.Id == id && !t.Isarchived);
        }

        public async Task<bool> CodeExistsAsync(string teamCode)
        {
            return await _context.Marketingteams
                .AnyAsync(t => t.Teamcode == teamCode);
        }

        public async Task<bool> CodeExistsForOtherAsync(
            string teamCode,
            long id)
        {
            return await _context.Marketingteams
                .AnyAsync(t =>
                    t.Teamcode == teamCode &&
                    t.Id != id);
        }

        public async Task AddAsync(Marketingteam team)
        {
            await _context.Marketingteams.AddAsync(team);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Marketingteam team)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Marketingteam team)
        {
            team.Isarchived = true;
            team.Status = 2;
            team.Modifieddate = DateTime.UtcNow;

            foreach (var member in team.Marketingteamdetails)
            {
                member.Isarchived = true;
                member.Status = 2;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Marketingteam?> GetActiveTeamForSalesmanAsync(
            long salesmanId)
        {
            return await _context.Marketingteams
                .Include(t => t.Marketingteamdetails)
                .FirstOrDefaultAsync(t =>
                    t.Status == 1 &&
                    !t.Isarchived &&
                    t.Marketingteamdetails.Any(d =>
                        d.Salesmanid == salesmanId &&
                        d.Status == 1 &&
                        !d.Isarchived));
        }
    }
}