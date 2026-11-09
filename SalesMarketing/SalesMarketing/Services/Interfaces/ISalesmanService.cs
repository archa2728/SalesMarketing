using SalesMarketing.DTOs;

namespace SalesMarketing.Services.Interfaces
{
    public interface ISalesmanService
    {
        //Task<List<SalesmanResponseDto>> GetAllAsync();
        Task<SalesmanPagedResponseDto> GetAllAsync(
    string? search,
    short? status,
    int pageNumber,
    int pageSize);

        Task<SalesmanResponseDto?> GetByIdAsync(long id);

        Task<SalesmanResponseDto> CreateAsync(
            CreateSalesmanDto dto);

        Task<bool> UpdateAsync(
            long id, UpdateSalesmanDto dto);

        Task<bool> UpdateStatusAsync(
            long id, UpdateSalesmanStatusDto dto);

        Task<bool> DeleteAsync(long id);

        Task<List<SalesmanLookupDto>> GetActiveSalesmenAsync();
    }
}
