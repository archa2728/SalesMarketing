
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;
using SalesMarketing.Exceptions;

namespace SalesMarketing.Services
{
    public class SalesmanRemovalRequestService
        : ISalesmanRemovalRequestService
    {
        private readonly SalesMarketingDbContext _context;
        private readonly ISalesmanRepository _salesmanRepository;
        private readonly IMarketingTeamRepository _marketingTeamRepository;
        private readonly ISalesmanRemovalRequestRepository _requestRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<SalesmanRemovalRequestService> _logger;

        public SalesmanRemovalRequestService(
            SalesMarketingDbContext context,
            ISalesmanRepository salesmanRepository,
            IMarketingTeamRepository marketingTeamRepository,
            ISalesmanRemovalRequestRepository requestRepository,
            IAuditLogService auditLogService,
            ILogger<SalesmanRemovalRequestService> logger)
        {
            _context = context;
            _salesmanRepository = salesmanRepository;
            _marketingTeamRepository = marketingTeamRepository;
            _requestRepository = requestRepository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // 1. Create a removal request
        public async Task<bool> CreateRequestAsync(
            long salesmanId,
            long requestedBy)
        {
            var salesman = await _salesmanRepository.GetByIdAsync(salesmanId);

            if (salesman == null)
                throw new KeyNotFoundException("Salesman not found.");

            if (salesman.Status != 1)
                throw new ConflictException(
                    "Only active salesmen can have a removal request.");

            var team = await _marketingTeamRepository
                .GetActiveTeamForSalesmanAsync(salesmanId);

            if (team == null)
                throw new ConflictException(
                    "This salesman is not assigned to an active marketing team.");

            if (await _requestRepository.HasPendingRequestAsync(salesmanId))
                throw new ConflictException(
                    "A pending removal request already exists for this salesman.");

            var request = new Salesmanremovalrequest
            {
                Salesmanid = salesmanId,
                Marketingteamid = team.Id,
                Requestedby = requestedBy,
                Status = 1
            };

            await _requestRepository.AddAsync(request);

            await _auditLogService.WriteAsync(
                "Create",
                "SalesmanRemovalRequest",
                request.Id,
                newValues: new
                {
                    request.Salesmanid,
                    request.Marketingteamid,
                    request.Requestedby,
                    request.Status
                });

            _logger.LogInformation(
                "Salesman removal request {RequestId} created for salesman {SalesmanId} by user {RequestedBy}.",
                request.Id,
                salesmanId,
                requestedBy);

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
            long reviewedBy,
            long? replacementTeamLeaderId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _requestRepository.GetByIdAsync(requestId);

                if (request == null)
                    return false;

                if (request.Status != 1)
                    throw new ConflictException(
                        "This removal request has already been reviewed.");

                var salesman = await _context.Salesmen
                    .FirstOrDefaultAsync(s =>
                        s.Id == request.Salesmanid &&
                        !s.Isarchived &&
                        s.Status == 1);

                if (salesman == null)
                    throw new ConflictException(
                        "The salesman is no longer active. Approval cannot proceed.");

                var team = await _context.Marketingteams
                    .FirstOrDefaultAsync(t =>
                        t.Id == request.Marketingteamid &&
                        !t.Isarchived &&
                        t.Status == 1);

                if (team == null)
                    throw new ConflictException(
                        "The marketing team is no longer active. Approval cannot proceed.");

                // Load all active, non-archived members of this team.
                var activeMembers = await _context.Marketingteamdetails
                    .Where(d =>
                        d.Marketingteamid == request.Marketingteamid &&
                        d.Status == 1 &&
                        !d.Isarchived)
                    .ToListAsync();

                var member = activeMembers.FirstOrDefault(d =>
                    d.Salesmanid == request.Salesmanid);

                if (member == null)
                    throw new ConflictException(
                        "The salesman is no longer an active member of this team.");

                // Capture original values before making changes.
                var oldSalesmanValues = new
                {
                    salesman.Status,
                    salesman.Isarchived
                };

                var oldMemberValues = new
                {
                    member.Role,
                    member.Status,
                    member.Isarchived
                };

                var oldTeamValues = new
                {
                    team.Teamleaderid
                };

                // TeamLeader = 1
                const short teamLeaderRole = 1;

                bool isTeamLeader = member.Role == teamLeaderRole;

                var currentLeaders = activeMembers
                    .Where(d => d.Role == teamLeaderRole)
                    .ToList();

                object? oldReplacementValues = null;
                Marketingteamdetail? replacementMember = null;

                if (isTeamLeader)
                {
                    if (currentLeaders.Count != 1)
                        throw new ConflictException(
                            "The team must have exactly one active Team Leader before removal.");

                    if (!replacementTeamLeaderId.HasValue)
                        throw new ConflictException(
                            "Select a replacement Team Leader before approving this removal.");

                    if (replacementTeamLeaderId.Value == request.Salesmanid)
                        throw new ConflictException(
                            "The removed salesman cannot be selected as the replacement Team Leader.");

                    // The replacement must already be an active member of this same team.
                    replacementMember = activeMembers.FirstOrDefault(d =>
                        d.Salesmanid == replacementTeamLeaderId.Value);

                    if (replacementMember == null)
                        throw new ConflictException(
                            "The replacement must be an active member of the same marketing team.");

                    // Confirm that the replacement salesman is also active.
                    bool replacementSalesmanIsActive =
                        await _context.Salesmen.AnyAsync(s =>
                            s.Id == replacementTeamLeaderId.Value &&
                            s.Status == 1 &&
                            !s.Isarchived);

                    if (!replacementSalesmanIsActive)
                        throw new ConflictException(
                            "The selected replacement salesman is not active.");

                    // Capture original values BEFORE changing the role.
                    oldReplacementValues = new
                    {
                        replacementMember.Role,
                        replacementMember.Status
                    };

                    // Assign Team Leader role to the replacement.
                    replacementMember.Role = teamLeaderRole;
                    team.Teamleaderid = replacementMember.Salesmanid;
                }
                else if (replacementTeamLeaderId.HasValue)
                {
                    throw new ConflictException(
                        "A replacement Team Leader is only required when removing the current Team Leader.");
                }

                // Deactivate and archive the removed membership.
                member.Status = 2;
                member.Isarchived = true;
                member.Wasactivebeforeteamdeactivation = false;

                // Deactivate and archive the removed salesman.
                salesman.Status = 2;
                salesman.Isarchived = true;
                salesman.Modifieddate = DateTime.UtcNow;

                // Verify that exactly one active Team Leader remains.
                int remainingLeaders = activeMembers.Count(d =>
                    d.Status == 1 &&
                    !d.Isarchived &&
                    d.Role == teamLeaderRole);

                if (remainingLeaders != 1)
                    throw new ConflictException(
                        "The team must have exactly one active Team Leader after removal.");

                // Capture new values.
                var newSalesmanValues = new
                {
                    salesman.Status,
                    salesman.Isarchived
                };

                var newMemberValues = new
                {
                    member.Role,
                    member.Status,
                    member.Isarchived
                };

                // Mark the request as approved.
                var oldRequestValues = new
                {
                    request.Status
                };

                request.Status = 2;
                request.Reviewedby = reviewedBy;
                request.Revieweddate = DateTime.UtcNow;
                request.Rejectionreason = null;

                var newRequestValues = new
                {
                    request.Status,
                    request.Reviewedby,
                    request.Revieweddate
                };

                // Save entity changes.
                await _context.SaveChangesAsync();

                // Audit the approved request.
                await _auditLogService.WriteAsync(
                    "Approve",
                    "SalesmanRemovalRequest",
                    request.Id,
                    oldRequestValues,
                    newRequestValues);

                // Audit the removed salesman.
                await _auditLogService.WriteAsync(
                    "Archive",
                    "Salesman",
                    salesman.Id,
                    oldSalesmanValues,
                    newSalesmanValues);

                // Audit the removed team membership.
                await _auditLogService.WriteAsync(
                    "Remove",
                    "MarketingTeamMember",
                    member.Id,
                    oldMemberValues,
                    newMemberValues);

                // Audit replacement Team Leader, if applicable.
                if (replacementMember != null)
                {
                    await _auditLogService.WriteAsync(
                        "Update",
                        "MarketingTeamMember",
                        replacementMember.Id,
                        oldReplacementValues,
                        new
                        {
                            replacementMember.Role,
                            replacementMember.Status
                        });

                    await _auditLogService.WriteAsync(
                        "Update",
                        "MarketingTeam",
                        team.Id,
                        oldTeamValues,
                        new
                        {
                            team.Teamleaderid
                        });
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Salesman removal request {RequestId} approved by user {ReviewedBy} for salesman {SalesmanId}.",
                    request.Id,
                    reviewedBy,
                    request.Salesmanid);

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
                throw new ConflictException(
                    "This removal request has already been reviewed.");

            var oldValues = new
            {
                request.Status,
                request.Rejectionreason
            };

            // Update only the request.
            // Salesman and team membership remain unchanged.
            request.Status = 3;
            request.Reviewedby = reviewedBy;
            request.Revieweddate = DateTime.UtcNow;
            request.Rejectionreason = rejectionReason;

            await _requestRepository.UpdateAsync(request);

            await _auditLogService.WriteAsync(
                "Reject",
                "SalesmanRemovalRequest",
                request.Id,
                oldValues,
                new
                {
                    request.Status,
                    request.Reviewedby,
                    request.Revieweddate,
                    request.Rejectionreason
                });

            _logger.LogInformation(
                "Salesman removal request {RequestId} rejected by user {ReviewedBy}.",
                request.Id,
                reviewedBy);

            return true;
        }
    }
}
