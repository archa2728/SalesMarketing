
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

        // Get all teams that are not archived.
        public async Task<List<Marketingteam>> GetAllAsync()
        {
            return await _context.Marketingteams
                .Where(t => !t.Isarchived)
                .Include(t => t.Marketingteamdetails
                    .Where(d => !d.Isarchived))
                .AsNoTracking()
                .OrderBy(t => t.Teamcode)
                .ToListAsync();
        }

        // Get one team with its non-archived members.
        public async Task<Marketingteam?> GetByIdAsync(long id)
        {
            return await _context.Marketingteams
                .Include(t => t.Marketingteamdetails
                    .Where(d => !d.Isarchived))
                .FirstOrDefaultAsync(t =>
                    t.Id == id && !t.Isarchived);
        }

        // Check whether a team code already exists.
        public async Task<bool> CodeExistsAsync(string teamCode)
        {
            return await _context.Marketingteams
                .AnyAsync(t => t.Teamcode == teamCode);
        }

        // Check whether another team uses this code.
        public async Task<bool> CodeExistsForOtherAsync(
            string teamCode,
            long id)
        {
            return await _context.Marketingteams
                .AnyAsync(t =>
                    t.Teamcode == teamCode &&
                    t.Id != id);
        }

        // Add a team and its detail rows.
        public async Task AddAsync(Marketingteam team)
        {
            await _context.Marketingteams.AddAsync(team);
            await _context.SaveChangesAsync();
        }

        // Save changes to the tracked team and its detail rows.
        public async Task UpdateAsync(Marketingteam team)
        {
            await _context.SaveChangesAsync();
        }

        // Archive the team and all its member detail rows.
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

