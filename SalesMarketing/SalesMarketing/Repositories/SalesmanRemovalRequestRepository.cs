
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;

namespace SalesMarketing.Repositories
{
    public class SalesmanRemovalRequestRepository
        : ISalesmanRemovalRequestRepository
    {
        private readonly SalesMarketingDbContext _context;

        public SalesmanRemovalRequestRepository(
            SalesMarketingDbContext context)
        {
            _context = context;
        }

        // Create a new removal request
        public async Task AddAsync(Salesmanremovalrequest request)
        {
            await _context.Salesmanremovalrequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        // Check whether a pending request already exists
        public async Task<bool> HasPendingRequestAsync(long salesmanId)
        {
            return await _context.Salesmanremovalrequests
                .AnyAsync(r =>
                    r.Salesmanid == salesmanId &&
                    r.Status == 1);
        }

        // Get pending requests for a particular marketing team

        public async Task<List<SalesmanRemovalRequestResponseDto>>
            GetPendingRequestsAsync()
        {
            var requests =
                await (from request in _context.Salesmanremovalrequests

                       join salesman in _context.Salesmen
                           on request.Salesmanid equals salesman.Id

                       join team in _context.Marketingteams
                           on request.Marketingteamid equals team.Id

                       join requester in _context.Appusers
                           on request.Requestedby equals requester.Id

                       where request.Status == 1

                       orderby request.Requesteddate descending

                       select new SalesmanRemovalRequestResponseDto
                       {
                           Id = request.Id,

                           SalesmanId = salesman.Id,
                           SalesmanCode = salesman.Salesmancode,
                           SalesmanName = salesman.Salesmanname,

                           MarketingTeamId = team.Id,
                           TeamCode = team.Teamcode,
                           TeamName = team.Teamname,

                           RequestedBy = requester.Id,
                           RequestedByUsername = requester.Username,
                           RequestedDate = request.Requesteddate,

                           Status = request.Status,

                           ReviewedBy = request.Reviewedby,
                           ReviewedDate = request.Revieweddate,

                           RejectionReason = request.Rejectionreason
                       })
                    .AsNoTracking()
                    .ToListAsync();

            return requests;
        }


        // Get a request by its ID
        public async Task<Salesmanremovalrequest?> GetByIdAsync(long id)
        {
            return await _context.Salesmanremovalrequests
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Save changes to a request
        public async Task UpdateAsync(Salesmanremovalrequest request)
        {
            await _context.SaveChangesAsync();
        }
    }
}
