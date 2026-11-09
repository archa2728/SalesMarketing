
using SalesMarketing.DTOs;
using SalesMarketing.Models;

namespace SalesMarketing.Repositories.Interfaces
{
    public interface ISalesmanRemovalRequestRepository
    {
        // Create a new removal request
        Task AddAsync(Salesmanremovalrequest request);

        // Check whether a pending request already exists
        Task<bool> HasPendingRequestAsync(long salesmanId);

        // Get pending requests with salesman, team and requester details
        Task<List<SalesmanRemovalRequestResponseDto>> GetPendingRequestsAsync();

        // Get a request by its ID
        Task<Salesmanremovalrequest?> GetByIdAsync(long id);

        // Save changes to a request
        Task UpdateAsync(Salesmanremovalrequest request);
    }
}
