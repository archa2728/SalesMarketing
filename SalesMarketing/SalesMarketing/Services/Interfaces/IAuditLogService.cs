namespace SalesMarketing.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task WriteAsync(
            string action,
            string entityName,
            long entityId,
            object? oldValues = null,
            object? newValues = null);
    }
}