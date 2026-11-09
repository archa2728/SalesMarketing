using Microsoft.AspNetCore.Mvc;
using SalesMarketing.DTOs;
using SalesMarketing.Models;
using SalesMarketing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SalesMarketing.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SalesmanController:ControllerBase
    {
        private readonly ISalesmanService _salesmanService;
        private readonly ISalesmanRemovalRequestService _removalRequestService;

        public SalesmanController(ISalesmanService salesmanService, ISalesmanRemovalRequestService removalRequestService)
        {
            _salesmanService = salesmanService;
            _removalRequestService = removalRequestService;
        }

        // GET: api/Salesman
        [Authorize(Roles = "1")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] string? search,
    [FromQuery] short? status,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Invalid pagination values."
                });
            }

            if (status.HasValue && status.Value != 1 && status.Value != 2)
            {
                return BadRequest(new
                {
                    message = "Invalid status. Use 1 for Active or 2 for Inactive."
                });
            }

            var salesmen = await _salesmanService.GetAllAsync(
                search,
                status,
                pageNumber,
                pageSize);

            return Ok(salesmen);
        }


        // GET: api/Salesman/5
        [Authorize(Roles = "1")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var salesman = await _salesmanService.GetByIdAsync(id);

            if (salesman == null)
                return NotFound(new { message = "Salesman not found." });

            return Ok(salesman);
        }

        // GET: api/Salesman/active
        [Authorize(Roles = "1,2")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSalesmen()
        {
            var salesmen = await _salesmanService.GetActiveSalesmenAsync();
            return Ok(salesmen);
        }

        // POST: api/Salesman
        [Authorize(Roles = "1")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateSalesmanDto dto)
        {
            try
            {
                var salesman = await _salesmanService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = salesman.Id },
                    salesman);
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

        // PUT: api/Salesman/5
        [Authorize(Roles = "1")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            UpdateSalesmanDto dto)
        {
            try
            {
                bool updated = await _salesmanService.UpdateAsync(id, dto);

                if (!updated)
                    return NotFound(new { message = "Salesman not found." });

                var salesman = await _salesmanService.GetByIdAsync(id);
                return Ok(salesman);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/Salesman/5/status
        [Authorize(Roles = "1")]
        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> UpdateStatus(
            long id,
            UpdateSalesmanStatusDto dto)
        {
            try
            {
                bool updated = await _salesmanService.UpdateStatusAsync(id, dto);

                if (!updated)
                    return NotFound(new { message = "Salesman not found." });

                return Ok(new { message = "Salesman status updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Salesman/5
        [Authorize(Roles = "1")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                bool deleted = await _salesmanService.DeleteAsync(id);

                if (!deleted)
                    return NotFound(new { message = "Salesman not found." });

                return Ok(new { message = "Salesman deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // POST: api/Salesman/5/removal-request
        [Authorize(Roles = "1")]
        [HttpPost("{id:long}/removal-request")]
        public async Task<IActionResult> CreateRemovalRequest(long id)
        {
            try
            {
                var userIdValue = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!long.TryParse(userIdValue, out long requestedBy))
                {
                    return Unauthorized(new
                    {
                        message = "Unable to identify the logged-in admin."
                    });
                }

                await _removalRequestService.CreateRequestAsync(
                    id,
                    requestedBy);

                return Ok(new
                {
                    message = "Removal request submitted successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

    }
}
