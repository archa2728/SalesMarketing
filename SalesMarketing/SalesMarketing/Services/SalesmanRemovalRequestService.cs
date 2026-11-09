
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;

namespace SalesMarketing.Services
{
    public class SalesmanRemovalRequestService
        : ISalesmanRemovalRequestService
    {
        private readonly SalesMarketingDbContext _context;
        private readonly ISalesmanRepository _salesmanRepository;
        private readonly IMarketingTeamRepository _marketingTeamRepository;
        private readonly ISalesmanRemovalRequestRepository _requestRepository;

        public SalesmanRemovalRequestService(
            SalesMarketingDbContext context,
            ISalesmanRepository salesmanRepository,
            IMarketingTeamRepository marketingTeamRepository,
            ISalesmanRemovalRequestRepository requestRepository)
        {
            _context = context;
            _salesmanRepository = salesmanRepository;
            _marketingTeamRepository = marketingTeamRepository;
            _requestRepository = requestRepository;
        }

        // 1. Create a removal request
        public async Task<bool> CreateRequestAsync(
            long salesmanId,
            long requestedBy)
        {
            // Find the salesman
            var salesman = await _salesmanRepository.GetByIdAsync(salesmanId);

            if (salesman == null)
                throw new KeyNotFoundException("Salesman not found.");

            if (salesman.Status != 1)
                throw new InvalidOperationException(
                    "Only active salesmen can have a removal request.");

            // Find the active team to which the salesman belongs
            var team = await _marketingTeamRepository
                .GetActiveTeamForSalesmanAsync(salesmanId);

            if (team == null)
                throw new InvalidOperationException(
                    "This salesman is not assigned to an active marketing team.");

            // Prevent duplicate pending requests
            if (await _requestRepository.HasPendingRequestAsync(salesmanId))
                throw new InvalidOperationException(
                    "A pending removal request already exists for this salesman.");

            // Create request
            var request = new Salesmanremovalrequest
            {
                Salesmanid = salesmanId,
                Marketingteamid = team.Id,
                Requestedby = requestedBy,
                Status = 1
            };

            await _requestRepository.AddAsync(request);

            return true;
        }

        // 2. Get all pending removal requests
        public async Task<List<SalesmanRemovalRequestResponseDto>>
            GetPendingRequestsAsync()
        {
            return await _requestRepository.GetPendingRequestsAsync();
        }

        // 3. Approve a removal request
        public async Task<bool> ApproveRequestAsync(
            long requestId,
            long reviewedBy)
        {
            // Begin database transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Find the request
                var request = await _requestRepository.GetByIdAsync(requestId);

                if (request == null)
                    return false;

                if (request.Status != 1)
                    throw new InvalidOperationException(
                        "This removal request has already been reviewed.");

                // Find the salesman
                var salesman = await _context.Salesmen
                    .FirstOrDefaultAsync(s =>
                        s.Id == request.Salesmanid &&
                        !s.Isarchived &&
                        s.Status == 1);

                if (salesman == null)
                    throw new InvalidOperationException(
                        "The salesman is no longer active. Approval cannot proceed.");

                // Find the marketing team
                var team = await _context.Marketingteams
                    .FirstOrDefaultAsync(t =>
                        t.Id == request.Marketingteamid &&
                        !t.Isarchived &&
                        t.Status == 1);

                if (team == null)
                    throw new InvalidOperationException(
                        "The marketing team is no longer active. Approval cannot proceed.");

                // Find the active membership
                var member = await _context.Marketingteamdetails
                    .FirstOrDefaultAsync(d =>
                        d.Marketingteamid == request.Marketingteamid &&
                        d.Salesmanid == request.Salesmanid &&
                        d.Status == 1 &&
                        !d.Isarchived);

                if (member == null)
                    throw new InvalidOperationException(
                        "The salesman is no longer an active member of this team.");

                // Deactivate and archive the team membership
                member.Status = 2;
                member.Isarchived = true;

                // Deactivate and archive the salesman
                salesman.Status = 2;
                salesman.Isarchived = true;
                salesman.Modifieddate = DateTime.UtcNow;

                // Update the request
                request.Status = 2;
                request.Reviewedby = reviewedBy;
                request.Revieweddate = DateTime.UtcNow;
                request.Rejectionreason = null;

                // Save all changes together
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 4. Reject a removal request
        public async Task<bool> RejectRequestAsync(
            long requestId,
            long reviewedBy,
            string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new ArgumentException(
                    "Rejection reason is required.");

            rejectionReason = rejectionReason.Trim();

            if (rejectionReason.Length > 500)
                throw new ArgumentException(
                    "Rejection reason cannot exceed 500 characters.");

            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
                return false;

            if (request.Status != 1)
                throw new InvalidOperationException(
                    "This removal request has already been reviewed.");

            // Update only the request.
            // Salesman and team membership remain unchanged.
            request.Status = 3;
            request.Reviewedby = reviewedBy;
            request.Revieweddate = DateTime.UtcNow;
            request.Rejectionreason = rejectionReason;

            await _requestRepository.UpdateAsync(request);

            return true;
        }
    }
}
