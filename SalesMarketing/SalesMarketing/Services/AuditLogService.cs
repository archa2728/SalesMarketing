using System.Security.Claims;
using System.Text.Json;
using SalesMarketing.Data;
using SalesMarketing.Models;
using SalesMarketing.Services.Interfaces;

namespace SalesMarketing.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly SalesMarketingDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            SalesMarketingDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task WriteAsync(
            string action,
            string entityName,
            long entityId,
            object? oldValues = null,
            object? newValues = null)
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            long? userId = null;

            if (long.TryParse(userIdValue, out long parsedUserId))
            {
                userId = parsedUserId;
            }

            var auditLog = new AuditLog
            {
                Userid = userId,
                Action = action,
                Entityname = entityName,
                Entityid = entityId,

                Oldvalues = oldValues == null
                    ? null
                    : JsonSerializer.Serialize(oldValues),

                Newvalues = newValues == null
                    ? null
                    : JsonSerializer.Serialize(newValues),

                Createddate = DateTime.UtcNow
            };

            await _context.Auditlogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}