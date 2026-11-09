using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesMarketing.DTOs;
using SalesMarketing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SalesMarketing.Controllers
{
    [Authorize(Roles = "2")]
    [ApiController]
    [Route("api/[controller]")]
    public class MarketingTeamController : ControllerBase
    {
        private readonly IMarketingTeamService _service;
        private readonly ISalesmanRemovalRequestService _removalRequestService;

        public MarketingTeamController(
            IMarketingTeamService service, ISalesmanRemovalRequestService removalRequestService)
        {
            _service = service;
            _removalRequestService = removalRequestService;
        }

        // GET: api/MarketingTeam
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var teams = await _service.GetAllAsync();
            return Ok(teams);
        }

        // GET: api/MarketingTeam/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var team = await _service.GetByIdAsync(id);

            if (team == null)
                return NotFound(new
                {
                    message = "Marketing team not found."
                });

            return Ok(team);
        }

        // POST: api/MarketingTeam
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateMarketingTeamDto dto)
        {
            try
            {
                var team = await _service.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = team.Id },
                    team);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "A database constraint was violated. " +
                              "Check the team code and member assignments."
                });
            }
        }

        // PUT: api/MarketingTeam/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateMarketingTeamDto dto)
        {
            try
            {
                bool updated = await _service.UpdateAsync(id, dto);

                if (!updated)
                    return NotFound(new
                    {
                        message = "Marketing team not found."
                    });

                return Ok(new
                {
                    message = "Marketing team updated successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "A database constraint was violated."
                });
            }
        }

        // PATCH: api/MarketingTeam/1/status
        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> UpdateStatus(
            long id,
            [FromBody] UpdateMarketingTeamStatusDto dto)
        {
            try
            {
                bool updated = await _service.UpdateStatusAsync(
                    id, dto.Status);

                if (!updated)
                    return NotFound(new
                    {
                        message = "Marketing team not found."
                    });

                return Ok(new
                {
                    message = "Marketing team status updated successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/MarketingTeam/1
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            bool deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound(new
                {
                    message = "Marketing team not found."
                });

            return Ok(new
            {
                message = "Marketing team deleted successfully."
            });
        }

        [HttpPost("{teamId:long}/members")]
        public async Task<IActionResult> AddMember(
            long teamId,
            [FromBody] AddMarketingTeamMemberDto dto)
        {
            try
            {
                bool added = await _service.AddMemberAsync(teamId, dto);

                if (!added)
                    return NotFound(new
                    {
                        message = "Marketing team not found."
                    });

                return Ok(new
                {
                    message = "Salesman added to the marketing team successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "A database constraint was violated."
                });
            }
        }

        // GET: api/MarketingTeam/removal-requests
        [HttpGet("removal-requests")]
        public async Task<IActionResult> GetPendingRemovalRequests()
        {
            var requests =
                await _removalRequestService.GetPendingRequestsAsync();

            return Ok(requests);
        }

        // PUT: api/MarketingTeam/removal-requests/1/approve
        [HttpPut("removal-requests/{id:long}/approve")]
        public async Task<IActionResult> ApproveRemovalRequest(long id)
        {
            try
            {
                var userIdValue = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!long.TryParse(userIdValue, out long reviewedBy))
                {
                    return Unauthorized(new
                    {
                        message = "Unable to identify the logged-in admin."
                    });
                }

                bool approved =
                    await _removalRequestService.ApproveRequestAsync(
                        id, reviewedBy);

                if (!approved)
                {
                    return NotFound(new
                    {
                        message = "Removal request not found."
                    });
                }

                return Ok(new
                {
                    message = "Salesman removal request approved successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/MarketingTeam/removal-requests/1/reject
        [HttpPut("removal-requests/{id:long}/reject")]
        public async Task<IActionResult> RejectRemovalRequest(
            long id,
            [FromBody] RejectSalesmanRemovalRequestDto dto)
        {
            try
            {
                var userIdValue = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!long.TryParse(userIdValue, out long reviewedBy))
                {
                    return Unauthorized(new
                    {
                        message = "Unable to identify the logged-in admin."
                    });
                }

                bool rejected =
                    await _removalRequestService.RejectRequestAsync(
                        id,
                        reviewedBy,
                        dto.RejectionReason);

                if (!rejected)
                {
                    return NotFound(new
                    {
                        message = "Removal request not found."
                    });
                }

                return Ok(new
                {
                    message = "Salesman removal request rejected successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
