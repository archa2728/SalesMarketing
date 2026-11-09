
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;

namespace SalesMarketing.Services
{
    public class MarketingTeamService : IMarketingTeamService
    {
        private readonly IMarketingTeamRepository _repository;
        private readonly SalesMarketingDbContext _context;

        public MarketingTeamService(
            IMarketingTeamRepository repository,
            SalesMarketingDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<List<MarketingTeamResponseDto>> GetAllAsync()
        {
            var teams = await _repository.GetAllAsync();
            var result = new List<MarketingTeamResponseDto>();

            foreach (var team in teams)
            {
                result.Add(await MapToResponseAsync(team));
            }

            return result;
        }

        public async Task<MarketingTeamResponseDto?> GetByIdAsync(long id)
        {
            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return null;

            return await MapToResponseAsync(team);
        }

        public async Task<MarketingTeamResponseDto> CreateAsync(
            CreateMarketingTeamDto dto)
        {
            string code = dto.TeamCode.Trim().ToUpperInvariant();

            if (await _repository.CodeExistsAsync(code))
                throw new InvalidOperationException(
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

            // Save the team and its members together.
            await _repository.AddAsync(team);

            return await MapToResponseAsync(team);
        }

        public async Task<bool> UpdateAsync(
            long id,
            UpdateMarketingTeamDto dto)
        {
            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return false;

            ValidateMembers(dto.TeamLeaderId, dto.Members);

            await ValidateSalesmenAsync(
                dto.Members,
                id,
                dto.Status == 1);

            team.Teamname = dto.TeamName.Trim();
            team.Teamleaderid = dto.TeamLeaderId;
            team.Description = dto.Description;
            team.Status = dto.Status;
            team.Modifieddate = DateTime.UtcNow;

            var incomingSalesmanIds = dto.Members
                .Select(m => m.SalesmanId)
                .ToHashSet();

            // Archive existing members omitted from the submitted list.
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
            }

            // Save the team and member changes together.
            await _repository.UpdateAsync(team);

            return true;
        }

        public async Task<bool> UpdateStatusAsync(long id, short status)
        {
            if (status != 1 && status != 2)
                throw new ArgumentException(
                    "Status must be 1 (Active) or 2 (Inactive).");

            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return false;

            team.Status = status;
            team.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(team);

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var team = await _repository.GetByIdAsync(id);

            if (team == null)
                return false;

            await _repository.DeleteAsync(team);

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

            // Check that all salesmen exist and are not archived.
            var salesmen = await _context.Salesmen
                .Where(s =>
                    salesmanIds.Contains(s.Id) &&
                    !s.Isarchived)
                .ToListAsync();

            if (salesmen.Count != salesmanIds.Count)
                throw new ArgumentException(
                    "One or more salesmen do not exist or are archived.");

            // Only active salesmen can be assigned to a team.
            if (salesmen.Any(s => s.Status != 1))
                throw new ArgumentException(
                    "Only active salesmen can be assigned to a team.");

            // An inactive team cannot create an active-team conflict.
            if (!isActiveTeam)
                return;

            // Only incoming active memberships need conflict checking.
            var activeSalesmanIds = members
                .Where(m => m.Status == 1)
                .Select(m => m.SalesmanId)
                .Distinct()
                .ToList();

            if (!activeSalesmanIds.Any())
                return;

            // Find active, non-archived teams.
            var activeTeamIds = await _context.Marketingteams
                .Where(t =>
                    !t.Isarchived &&
                    t.Status == 1)
                .Select(t => t.Id)
                .ToListAsync();

            // Find salesmen assigned to another active team.
            var assignedSalesmanIds =
                await _context.Marketingteamdetails
                    .Where(d =>
                        !d.Isarchived &&
                        d.Status == 1 &&
                        activeTeamIds.Contains(d.Marketingteamid) &&
                        (!currentTeamId.HasValue ||
                         d.Marketingteamid != currentTeamId.Value) &&
                        activeSalesmanIds.Contains(d.Salesmanid))
                    .Select(d => d.Salesmanid)
                    .Distinct()
                    .ToListAsync();

            if (assignedSalesmanIds.Any())
                throw new InvalidOperationException(
                    "A salesman is already assigned to another active team.");
        }

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

        public async Task<bool> AddMemberAsync(
            long teamId,
            AddMarketingTeamMemberDto dto)
        {
            // Find the existing team.
            var team = await _repository.GetByIdAsync(teamId);

            if (team == null)
                return false;

            if (team.Status != 1)
                throw new InvalidOperationException(
                    "Cannot add members to an inactive team.");

            // Check whether the salesman exists and is active.
            var salesman = await _context.Salesmen
                .FirstOrDefaultAsync(s =>
                    s.Id == dto.SalesmanId &&
                    !s.Isarchived);

            if (salesman == null)
                throw new ArgumentException(
                    "Salesman does not exist.");

            if (salesman.Status != 1)
                throw new ArgumentException(
                    "Only active salesmen can be assigned.");

            // Check for duplicate membership in this team.
            bool alreadyMember = team.Marketingteamdetails.Any(d =>
                d.Salesmanid == dto.SalesmanId &&
                !d.Isarchived);

            if (alreadyMember)
                throw new InvalidOperationException(
                    "This salesman is already a member of the team.");

            // Check for another active membership only when
            // the new membership is also active.
            if (dto.Status == 1)
            {
                var activeTeamIds = await _context.Marketingteams
                    .Where(t =>
                        t.Status == 1 &&
                        !t.Isarchived)
                    .Select(t => t.Id)
                    .ToListAsync();

                bool assignedElsewhere =
                    await _context.Marketingteamdetails.AnyAsync(d =>
                        d.Salesmanid == dto.SalesmanId &&
                        d.Status == 1 &&
                        !d.Isarchived &&
                        activeTeamIds.Contains(d.Marketingteamid) &&
                        d.Marketingteamid != teamId);

                if (assignedElsewhere)
                    throw new InvalidOperationException(
                        "This salesman already belongs to another active team.");
            }

            // A team must have exactly one team leader.
            if (dto.Role == 1)
                throw new ArgumentException(
                    "This team already has a leader. Change the team leader separately.");

            // Add the new member.
            var member = new Marketingteamdetail
            {
                Marketingteamid = teamId,
                Salesmanid = dto.SalesmanId,
                Role = dto.Role,
                Targetamount = dto.TargetAmount,
                Status = dto.Status,
                Isarchived = false
            };

            team.Marketingteamdetails.Add(member);
            team.Modifieddate = DateTime.UtcNow;

            // Save the new member and modified team date.
            await _repository.UpdateAsync(team);

            return true;
        }
    }
}
