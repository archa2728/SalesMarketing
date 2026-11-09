
using SalesMarketing.DTOs;

namespace SalesMarketing.Services.Interfaces
{
    public interface ISalesmanRemovalRequestService
    {
        Task<bool> CreateRequestAsync(
            long salesmanId,
            long requestedBy);

        Task<List<SalesmanRemovalRequestResponseDto>>
            GetPendingRequestsAsync();

        Task<bool> ApproveRequestAsync(
            long requestId,
            long reviewedBy);

        Task<bool> RejectRequestAsync(
            long requestId,
            long reviewedBy,
            string rejectionReason);
    }
}
