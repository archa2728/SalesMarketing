using SalesMarketing.DTOs;

namespace SalesMarketing.Services.Interfaces
{
    public interface IMarketingTeamService
    {
        Task<List<MarketingTeamResponseDto>> GetAllAsync();

        Task<MarketingTeamResponseDto?> GetByIdAsync(long id);

        Task<MarketingTeamResponseDto> CreateAsync(
            CreateMarketingTeamDto dto);

        Task<bool> UpdateAsync(
            long id,
            UpdateMarketingTeamDto dto);

        Task<bool> UpdateStatusAsync(
            long id,
            short status);

        Task<bool> DeleteAsync(long id);

        Task<bool> AddMemberAsync(
    long teamId,
    AddMarketingTeamMemberDto dto);
    }
}
