using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;
using SalesMarketing.Exceptions;

namespace SalesMarketing.Services
{
    public class MarketingTeamService : IMarketingTeamService
    {
        private readonly IMarketingTeamRepository _repository;
        private readonly SalesMarketingDbContext _context;
        private readonly ILogger<MarketingTeamService> _logger;
        private readonly IAuditLogService _auditLogService;

        public MarketingTeamService(
            IMarketingTeamRepository repository,
            SalesMarketingDbContext context,
            ILogger<MarketingTeamService> logger,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // Get paginated marketing teams.
        public async Task<MarketingTeamPageResponseDto> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? search,
            short? status)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentException(
                    "Page number must be at least 1.");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentException(
                    "Page size must be between 1 and 100.");
            }

            if (status.HasValue && status.Value != 1 && status.Value != 2)
            {
                throw new ArgumentException(
                    "Status must be 1 (Active) or 2 (Inactive).");
            }

            var (teams, totalCount) = await _repository.GetPagedAsync(
                pageNumber,
                pageSize,
                search,
                status);

            // Collect the leader and member IDs for all teams on this page.
            var salesmanIds = teams
                .SelectMany(team =>
                    team.Marketingteamdetails
                        .Where(member => !member.Isarchived)
                        .Select(member => member.Salesmanid)
                        .Append(team.Teamleaderid))
                .Distinct()
                .ToList();

            // Fetch all required salesmen in one query instead of one query per team.
            var salesmen = await _context.Salesmen
                .AsNoTracking()
                .Where(s => salesmanIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var items = teams
                .Select(team => MapToResponse(team, salesmen))
                .ToList();

            return new MarketingTeamPageResponseDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<MarketingTeamResponseDto?> GetByIdAsync(long id)
        {
            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return null;

            return await MapToResponseAsync(team);
        }

        // Create a new marketing team.
        public async Task<MarketingTeamResponseDto> CreateAsync(
            CreateMarketingTeamDto dto)
        {
            string code = dto.TeamCode.Trim().ToUpperInvariant();

            if (await _repository.CodeExistsAsync(code))
                throw new ConflictException(
                    "This marketing team code already exists.");

            ValidateMembers(dto.TeamLeaderId, dto.Members);

            await ValidateSalesmenAsync(
                dto.Members,
                null,
                dto.Status == 1);

            var team = new Marketingteam
            {
                Teamcode = code,
                Teamname = dto.TeamName.Trim(),
                Teamleaderid = dto.TeamLeaderId,
                Description = dto.Description,
                Status = dto.Status,
                Createddate = DateTime.UtcNow,
                Isarchived = false
            };

            foreach (var member in dto.Members)
            {
                team.Marketingteamdetails.Add(new Marketingteamdetail
                {
                    Salesmanid = member.SalesmanId,
                    Role = member.Role,
                    Targetamount = member.TargetAmount,
                    Status = member.Status,
                    Isarchived = false
                });
            }

            await _repository.AddAsync(team);

            await _auditLogService.WriteAsync(
                action: "Create",
                entityName: "MarketingTeam",
                entityId: team.Id,
                newValues: new
                {
                    team.Teamcode,
                    team.Teamname,
                    team.Teamleaderid,
                    team.Description,
                    team.Status
                });

            foreach (var member in team.Marketingteamdetails)
            {
                await _auditLogService.WriteAsync(
                    action: "Add",
                    entityName: "TeamMember",
                    entityId: member.Id,
                    newValues: new
                    {
                        member.Marketingteamid,
                        member.Salesmanid,
                        member.Role,
                        member.Targetamount,
                        member.Status
                    });
            }

            _logger.LogInformation(
                "Marketing team {TeamId} with code {TeamCode} created successfully.",
                team.Id,
                team.Teamcode);

            return await MapToResponseAsync(team);
        }

        // Update marketing team and its members.
        public async Task<bool> UpdateAsync(
            long id,
            UpdateMarketingTeamDto dto)
        {
            var team = await _repository.GetByIdIncludingArchivedMembersAsync(id);

            if (team == null)
                return false;

            ValidateMembers(dto.TeamLeaderId, dto.Members);

            await ValidateSalesmenAsync(
                dto.Members,
                id,
                dto.Status == 1);

            var oldTeamValues = new
            {
                team.Teamname,
                team.Teamleaderid,
                team.Description,
                team.Status
            };

            var oldMembers = team.Marketingteamdetails.ToDictionary(
                d => d.Salesmanid,
                d => new
                {
                    d.Id,
                    d.Marketingteamid,
                    d.Salesmanid,
                    d.Role,
                    d.Targetamount,
                    d.Status,
                    d.Isarchived
                });

            short oldTeamStatus = team.Status;

            team.Teamname = dto.TeamName.Trim();
            team.Teamleaderid = dto.TeamLeaderId;
            team.Description = dto.Description;
            team.Status = dto.Status;
            team.Modifieddate = DateTime.UtcNow;

            var incomingSalesmanIds = dto.Members
                .Select(m => m.SalesmanId)
                .ToHashSet();

            foreach (var existing in team.Marketingteamdetails
                .Where(d => !d.Isarchived))
            {
                if (!incomingSalesmanIds.Contains(existing.Salesmanid))
                {
                    existing.Isarchived = true;
                    existing.Status = 2;
                }
            }

            foreach (var member in dto.Members)
            {
                var detail = team.Marketingteamdetails.FirstOrDefault(
                    d => d.Salesmanid == member.SalesmanId);

                if (detail == null)
                {
                    detail = new Marketingteamdetail
                    {
                        Salesmanid = member.SalesmanId,
                        Isarchived = false
                    };

                    team.Marketingteamdetails.Add(detail);
                }

                detail.Role = member.Role;
                detail.Targetamount = member.TargetAmount;
                detail.Status = member.Status;
                detail.Isarchived = false;
                detail.Wasactivebeforeteamdeactivation = false;
            }

            await _repository.UpdateAsync(team);

            var newTeamValues = new
            {
                team.Teamname,
                team.Teamleaderid,
                team.Description,
                team.Status
            };

            await _auditLogService.WriteAsync(
                action: oldTeamStatus != team.Status
                    ? "StatusChange"
                    : "Update",
                entityName: "MarketingTeam",
                entityId: team.Id,
                oldValues: oldTeamValues,
                newValues: newTeamValues);

            foreach (var member in team.Marketingteamdetails)
            {
                oldMembers.TryGetValue(member.Salesmanid, out var oldMember);

                string? memberAction = null;

                if (oldMember == null)
                {
                    memberAction = "Add";
                }
                else if (oldMember.Isarchived && !member.Isarchived)
                {
                    memberAction = "Restore";
                }
                else if (!oldMember.Isarchived && member.Isarchived)
                {
                    memberAction = "Remove";
                }
                else
                {
                    memberAction = "Update";
                }

                if (memberAction != null)
                {
                    await _auditLogService.WriteAsync(
                        action: memberAction,
                        entityName: "TeamMember",
                        entityId: member.Id,
                        oldValues: oldMember == null
                            ? null
                            : new
                            {
                                oldMember.Salesmanid,
                                oldMember.Role,
                                oldMember.Targetamount,
                                oldMember.Status,
                                oldMember.Isarchived
                            },
                        newValues: new
                        {
                            member.Salesmanid,
                            member.Role,
                            member.Targetamount,
                            member.Status,
                            member.Isarchived
                        });
                }
            }

            _logger.LogInformation(
                "Marketing team {TeamId} updated successfully.",
                team.Id);

            return true;
        }

        // Activate or deactivate marketing team.
        public async Task<bool> UpdateStatusAsync(long id, short status)
        {
            if (status != 1 && status != 2)
                throw new ArgumentException(
                    "Status must be 1 (Active) or 2 (Inactive).");

            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return false;

            short oldStatus = team.Status;

            var oldMemberStatuses = team.Marketingteamdetails
                .ToDictionary(
                    m => m.Id,
                    m => new
                    {
                        m.Status,
                        m.Wasactivebeforeteamdeactivation
                    });

            var currentTime = DateTime.UtcNow;

            if (status == 2 && team.Status == 1)
            {
                foreach (var member in team.Marketingteamdetails)
                {
                    if (member.Status == 1)
                    {
                        member.Wasactivebeforeteamdeactivation = true;
                        member.Status = 2;
                    }
                }
            }

            if (status == 1 && team.Status == 2)
            {
                var membersToActivate = team.Marketingteamdetails
                    .Where(m =>
                        m.Status == 1 ||
                        m.Wasactivebeforeteamdeactivation)
                    .ToList();

                foreach (var member in membersToActivate)
                {
                    if (member.Salesman == null ||
                        member.Salesman.Status != 1 ||
                        member.Salesman.Isarchived)
                    {
                        throw new ConflictException(
                            $"Salesman ID {member.Salesmanid} is inactive or archived. " +
                            "The marketing team cannot be activated.");
                    }
                }

                var teamLeaders = membersToActivate
                    .Where(m => m.Role == 1)
                    .ToList();

                if (teamLeaders.Count != 1)
                {
                    throw new ConflictException(
                        "The marketing team must have exactly one active Team Leader before activation.");
                }

                if (teamLeaders[0].Salesmanid != team.Teamleaderid)
                {
                    throw new ConflictException(
                        "The Team Leader reference does not match the active Team Leader membership.");
                }

                foreach (var member in team.Marketingteamdetails)
                {
                    if (member.Wasactivebeforeteamdeactivation)
                    {
                        member.Status = 1;
                        member.Wasactivebeforeteamdeactivation = false;
                    }
                }
            }

            team.Status = status;
            team.Modifieddate = currentTime;

            await _repository.UpdateAsync(team);

            await _auditLogService.WriteAsync(
                action: "StatusChange",
                entityName: "MarketingTeam",
                entityId: team.Id,
                oldValues: new
                {
                    Status = oldStatus
                },
                newValues: new
                {
                    Status = team.Status
                });

            foreach (var member in team.Marketingteamdetails)
            {
                if (oldMemberStatuses.TryGetValue(
                    member.Id, out var oldMember))
                {
                    if (oldMember.Status != member.Status ||
                        oldMember.Wasactivebeforeteamdeactivation !=
                        member.Wasactivebeforeteamdeactivation)
                    {
                        await _auditLogService.WriteAsync(
                            action: "StatusChange",
                            entityName: "TeamMember",
                            entityId: member.Id,
                            oldValues: new
                            {
                                oldMember.Status,
                                oldMember.Wasactivebeforeteamdeactivation
                            },
                            newValues: new
                            {
                                member.Status,
                                member.Wasactivebeforeteamdeactivation
                            });
                    }
                }
            }

            _logger.LogInformation(
                "Marketing team {TeamId} status changed to {Status}.",
                team.Id,
                team.Status);

            return true;
        }

        // Archive marketing team.
        public async Task<bool> DeleteAsync(long id)
        {
            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return false;

            var oldValues = new
            {
                team.Teamcode,
                team.Teamname,
                team.Teamleaderid,
                team.Status,
                team.Isarchived
            };

            await _repository.DeleteAsync(team);

            await _auditLogService.WriteAsync(
                action: "Archive",
                entityName: "MarketingTeam",
                entityId: team.Id,
                oldValues: oldValues,
                newValues: new
                {
                    Status = 2,
                    Isarchived = true
                });

            _logger.LogInformation(
                "Marketing team {TeamId} archived successfully.",
                team.Id);

            return true;
        }

        private static void ValidateMembers(
            long teamLeaderId,
            List<MarketingTeamMemberDto> members)
        {
            if (members == null || members.Count == 0)
                throw new ArgumentException(
                    "A marketing team must have at least one member.");

            if (members.Any(m => m.SalesmanId <= 0))
                throw new ArgumentException(
                    "Every member must have a valid salesman ID.");

            if (members.Select(m => m.SalesmanId).Distinct().Count()
                != members.Count)
            {
                throw new ArgumentException(
                    "A salesman cannot appear more than once in a team.");
            }

            if (members.Any(m => m.Role < 1 || m.Role > 4))
                throw new ArgumentException(
                    "Member role must be between 1 and 4.");

            if (members.Any(m => m.Status < 1 || m.Status > 2))
                throw new ArgumentException(
                    "Member status must be 1 or 2.");

            if (members.Any(m => m.TargetAmount < 0))
                throw new ArgumentException(
                    "Target amount cannot be negative.");

            var leaders = members
                .Where(m => m.Role == 1)
                .ToList();

            if (leaders.Count != 1 ||
                leaders[0].SalesmanId != teamLeaderId)
            {
                throw new ArgumentException(
                    "Exactly one TeamLeader row is required, " +
                    "and it must match TeamLeaderId.");
            }
        }

        // Validate salesman existence, status and active team membership.
        private async Task ValidateSalesmenAsync(
            List<MarketingTeamMemberDto> members,
            long? currentTeamId,
            bool isActiveTeam)
        {
            var salesmanIds = members
                .Select(m => m.SalesmanId)
                .Distinct()
                .ToList();

            var salesmen = await _context.Salesmen
                .Where(s =>
                    salesmanIds.Contains(s.Id) &&
                    !s.Isarchived)
                .ToListAsync();

            if (salesmen.Count != salesmanIds.Count)
                throw new ArgumentException(
                    "One or more salesmen do not exist or are archived.");

            var activeMembershipSalesmanIds = members
                .Where(m => m.Status == 1)
                .Select(m => m.SalesmanId)
                .Distinct()
                .ToList();

            var inactiveSalesmanIds = salesmen
                .Where(s =>
                    activeMembershipSalesmanIds.Contains(s.Id) &&
                    s.Status != 1)
                .Select(s => s.Id)
                .ToList();

            if (inactiveSalesmanIds.Any())
                throw new ArgumentException(
                    "Only active salesmen can have active team memberships.");

            if (!isActiveTeam)
                return;

            if (!activeMembershipSalesmanIds.Any())
                return;

            var activeTeamIds = await _context.Marketingteams
                .Where(t =>
                    !t.Isarchived &&
                    t.Status == 1)
                .Select(t => t.Id)
                .ToListAsync();

            var assignedSalesmanIds =
                await _context.Marketingteamdetails
                    .Where(d =>
                        !d.Isarchived &&
                        d.Status == 1 &&
                        activeTeamIds.Contains(d.Marketingteamid) &&
                        (!currentTeamId.HasValue ||
                         d.Marketingteamid != currentTeamId.Value) &&
                        activeMembershipSalesmanIds.Contains(d.Salesmanid))
                    .Select(d => d.Salesmanid)
                    .Distinct()
                    .ToListAsync();

            if (assignedSalesmanIds.Any())
                throw new ConflictException(
                    "A salesman is already assigned to another active team.");
        }

        // Map one team using a single salesman lookup.
        private async Task<MarketingTeamResponseDto> MapToResponseAsync(
            Marketingteam team)
        {
            var members = team.Marketingteamdetails
                .Where(d => !d.Isarchived)
                .ToList();

            var salesmanIds = members
                .Select(m => m.Salesmanid)
                .Append(team.Teamleaderid)
                .Distinct()
                .ToList();

            var salesmen = await _context.Salesmen
                .AsNoTracking()
                .Where(s => salesmanIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            return MapToResponse(team, salesmen);
        }

        // Map a team using the already-loaded salesman dictionary.
        private MarketingTeamResponseDto MapToResponse(
            Marketingteam team,
            Dictionary<long, Salesman> salesmen)
        {
            var members = team.Marketingteamdetails
                .Where(d => !d.Isarchived)
                .ToList();

            salesmen.TryGetValue(team.Teamleaderid, out var leader);

            var response = new MarketingTeamResponseDto
            {
                Id = team.Id,
                TeamCode = team.Teamcode,
                TeamName = team.Teamname,
                TeamLeaderId = team.Teamleaderid,
                TeamLeaderName = leader?.Salesmanname,
                Description = team.Description,
                Status = team.Status,
                CreatedDate = team.Createddate,
                ModifiedDate = team.Modifieddate,
                TotalTargetAmount = members.Sum(m => m.Targetamount)
            };

            foreach (var member in members)
            {
                salesmen.TryGetValue(member.Salesmanid, out var salesman);

                response.Members.Add(new MarketingTeamMemberResponseDto
                {
                    Id = member.Id,
                    SalesmanId = member.Salesmanid,
                    SalesmanCode = salesman?.Salesmancode ?? "",
                    SalesmanName = salesman?.Salesmanname ?? "",
                    Role = member.Role,
                    TargetAmount = member.Targetamount,
                    Status = member.Status
                });
            }

            return response;
        }

        // Add a new member or restore a previously archived member.
        public async Task<bool> AddMemberAsync(
            long teamId,
            AddMarketingTeamMemberDto dto)
        {
            var team = await _repository.GetByIdIncludingArchivedMembersAsync(teamId);

            if (team == null)
                return false;

            bool isTeamInactive = team.Status == 2;

            if (isTeamInactive && dto.Role != 1)
            {
                throw new ConflictException(
                    "Only a replacement Team Leader can be added to an inactive team.");
            }

            var salesman = await _context.Salesmen
                .FirstOrDefaultAsync(s =>
                    s.Id == dto.SalesmanId &&
                    !s.Isarchived &&
                    s.Status == 1);

            if (salesman == null)
                throw new ArgumentException(
                    "Only active, non-archived salesmen can be assigned.");

            var existingMember = team.Marketingteamdetails
                .FirstOrDefault(d => d.Salesmanid == dto.SalesmanId);

            if (existingMember != null && !existingMember.Isarchived)
            {
                throw new ConflictException(
                    "This salesman is already a member of the team.");
            }

            if (dto.Status == 1)
            {
                bool assignedElsewhere =
                    await _context.Marketingteamdetails.AnyAsync(d =>
                        d.Salesmanid == dto.SalesmanId &&
                        d.Status == 1 &&
                        !d.Isarchived &&
                        d.Marketingteam.Status == 1 &&
                        !d.Marketingteam.Isarchived &&
                        d.Marketingteamid != teamId);

                if (assignedElsewhere)
                {
                    throw new ConflictException(
                        "This salesman already belongs to another active team.");
                }
            }

            if (dto.Role == 1)
            {
                bool existingActiveLeader = team.Marketingteamdetails.Any(d =>
                    d.Role == 1 &&
                    d.Status == 1 &&
                    !d.Isarchived);

                if (existingActiveLeader)
                {
                    throw new ConflictException(
                        "This team already has an active Team Leader.");
                }

                if (dto.Status != 1)
                {
                    throw new ArgumentException(
                        "The replacement Team Leader must have active membership.");
                }

                team.Teamleaderid = dto.SalesmanId;
            }

            var oldMemberValues = existingMember == null
                ? null
                : new
                {
                    existingMember.Salesmanid,
                    existingMember.Role,
                    existingMember.Targetamount,
                    existingMember.Status,
                    existingMember.Isarchived
                };

            string auditAction;

            if (existingMember != null)
            {
                auditAction = "Restore";

                existingMember.Role = dto.Role;
                existingMember.Targetamount = dto.TargetAmount;
                existingMember.Status = dto.Status;
                existingMember.Isarchived = false;
                existingMember.Wasactivebeforeteamdeactivation = false;
            }
            else
            {
                auditAction = "Add";

                var member = new Marketingteamdetail
                {
                    Marketingteamid = teamId,
                    Salesmanid = dto.SalesmanId,
                    Role = dto.Role,
                    Targetamount = dto.TargetAmount,
                    Status = dto.Status,
                    Isarchived = false,
                    Wasactivebeforeteamdeactivation = false
                };

                team.Marketingteamdetails.Add(member);
                existingMember = member;
            }

            team.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(team);

            await _auditLogService.WriteAsync(
                action: auditAction,
                entityName: "TeamMember",
                entityId: existingMember.Id,
                oldValues: oldMemberValues,
                newValues: new
                {
                    existingMember.Marketingteamid,
                    existingMember.Salesmanid,
                    existingMember.Role,
                    existingMember.Targetamount,
                    existingMember.Status,
                    existingMember.Isarchived
                });

            _logger.LogInformation(
                "Salesman {SalesmanId} added or restored as a member of marketing team {TeamId}.",
                dto.SalesmanId,
                teamId);

            return true;
        }
    }
}