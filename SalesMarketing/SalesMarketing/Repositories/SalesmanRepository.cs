using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;

namespace SalesMarketing.Repositories
{
    public class SalesmanRepository : ISalesmanRepository
    {
        private readonly SalesMarketingDbContext _context;

        public SalesmanRepository(SalesMarketingDbContext context)
        {
            _context = context;
        }

        public async Task<List<Salesman>> GetAllAsync()
        {
            return await _context.Salesmen
                .Where(s => !s.Isarchived)
                .AsNoTracking()
                .OrderBy(s => s.Salesmanname)
                .ToListAsync();
        }

        public async Task<Salesman?> GetByIdAsync(long id)
        {
            return await _context.Salesmen
                .FirstOrDefaultAsync(s => s.Id == id && !s.Isarchived);
        }
        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.Salesmen
                .AnyAsync(s => s.Salesmancode == code);
        }

        public async Task<bool> CodeExistsForOtherAsync(string code, long id)
        {
            return await _context.Salesmen
                .AnyAsync(s => s.Salesmancode == code
                            && s.Id != id);
        }

        // Add a salesman
        public async Task AddAsync(Salesman salesman)
        {
            await _context.Salesmen.AddAsync(salesman);
            await _context.SaveChangesAsync();
        }

        // Update a salesman
        public async Task UpdateAsync(Salesman salesman)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsAssignedToTeamAsync(long salesmanId)
        {
            return await _context.Marketingteamdetails
                .AnyAsync(d => d.Salesmanid == salesmanId
                            && !d.Isarchived);
        }

        public async Task<bool> HasActiveMembershipInActiveTeamAsync(long salesmanId)
        {
            return await _context.Marketingteamdetails
                .AnyAsync(d =>
                    d.Salesmanid == salesmanId &&
                    d.Status == 1 &&
                    !d.Isarchived &&
                    d.Marketingteam.Status == 1 &&
                    !d.Marketingteam.Isarchived);
        }

        public async Task DeleteAsync(Salesman salesman)
        {
            salesman.Isarchived = true;
            salesman.Status = 2; // Inactive

            // Archive all remaining active membership records.
            var memberships = await _context.Marketingteamdetails
                .Where(d => d.Salesmanid == salesman.Id && !d.Isarchived)
                .ToListAsync();

            foreach (var member in memberships)
            {
                member.Isarchived = true;
                member.Status = 2;
                member.Wasactivebeforeteamdeactivation = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<(List<Salesman> Items, int TotalCount)> GetPagedAsync(
    string? search,
    short? status,
    int pageNumber,
    int pageSize)
        {
            var query = _context.Salesmen
                .Where(s => !s.Isarchived)
                .AsNoTracking();


            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                string searchPattern = $"%{search}%";

                query = query.Where(s =>
                    EF.Functions.ILike(s.Salesmancode, searchPattern) ||
                    EF.Functions.ILike(s.Salesmanname, searchPattern) ||
                    (s.Mobileno != null &&
                     EF.Functions.ILike(s.Mobileno, searchPattern))
                );
            }

            // Filter by status: 1 = Active, 2 = Inactive
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // Get total count before pagination
            int totalCount = await query.CountAsync();

            // Get only the requested page
            var items = await query
               .OrderBy(s => s.Createddate)
               .ThenBy(s => s.Id)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            return (items, totalCount);
        }


        public async Task<bool> EmailExistsAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await _context.Salesmen
                .AnyAsync(s =>
                    s.Email != null &&
                    s.Email.ToLower() == normalizedEmail);
        }

        public async Task<bool> EmailExistsForOtherAsync(
            string email, long id)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await _context.Salesmen
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Email != null &&
                    s.Email.ToLower() == normalizedEmail);
        }

        public async Task<bool> MobileExistsAsync(string mobileNo)
        {
            var normalizedMobile = mobileNo.Trim();

            return await _context.Salesmen
                .AnyAsync(s =>
                    s.Mobileno != null &&
                    s.Mobileno.Trim() == normalizedMobile);
        }

        public async Task<bool> MobileExistsForOtherAsync(
            string mobileNo, long id)
        {
            var normalizedMobile = mobileNo.Trim();

            return await _context.Salesmen
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Mobileno != null &&
                    s.Mobileno.Trim() == normalizedMobile);
        }

    }
}
