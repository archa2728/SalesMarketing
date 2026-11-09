using SalesMarketing.DTOs;
using SalesMarketing.Enums;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;

namespace SalesMarketing.Services
{
    public class SalesmanService:ISalesmanService
    {
        private readonly ISalesmanRepository _repository;

        public SalesmanService(ISalesmanRepository repository)
        {
            _repository = repository;
        }

        // Get all salesmen

        public async Task<SalesmanPagedResponseDto> GetAllAsync(
    string? search,
    short? status,
    int pageNumber,
    int pageSize)
        {
            var result = await _repository.GetPagedAsync(
                search,
                status,
                pageNumber,
                pageSize);

            return new SalesmanPagedResponseDto
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount
            };
        }

        // Get one salesman by ID
        public async Task<SalesmanResponseDto?> GetByIdAsync(long id)
        {
            var salesman = await _repository.GetByIdAsync(id);

            return salesman == null ? null : MapToResponse(salesman);
        }

        // Create a new salesman
        public async Task<SalesmanResponseDto> CreateAsync(
            CreateSalesmanDto dto)
        {
            string code = dto.SalesmanCode.Trim().ToUpperInvariant();

            if (await _repository.CodeExistsAsync(code))
            {
                throw new InvalidOperationException(
                    "A salesman with this code already exists.");
            }

            ValidateJoiningDate(dto.JoiningDate);

            var salesman = new Salesman
            {
                Salesmancode = code,
                Salesmanname = dto.SalesmanName.Trim(),
                Mobileno = CleanOptionalValue(dto.MobileNo),
                Email = CleanOptionalValue(dto.Email),
                Joiningdate = dto.JoiningDate,
                Status = dto.Status
            };

            await _repository.AddAsync(salesman);

            return MapToResponse(salesman);
        }

        // Update salesman details
        public async Task<bool> UpdateAsync(
            long id, UpdateSalesmanDto dto)
        {
            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            ValidateJoiningDate(dto.JoiningDate);

            salesman.Salesmanname = dto.SalesmanName.Trim();
            salesman.Mobileno = CleanOptionalValue(dto.MobileNo);
            salesman.Email = CleanOptionalValue(dto.Email);
            salesman.Joiningdate = dto.JoiningDate;
            salesman.Status = dto.Status;
            salesman.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(salesman);

            return true;
        }

        // Activate or deactivate a salesman
        public async Task<bool> UpdateStatusAsync(
            long id, UpdateSalesmanStatusDto dto)
        {
            if (dto.Status != (short)RecordStatus.Active &&
                dto.Status != (short)RecordStatus.Inactive)
            {
                throw new ArgumentException("Invalid salesman status.");
            }

            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            salesman.Status = dto.Status;
            salesman.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(salesman);

            return true;
        }

        // Delete a salesman if not assigned to a team
        public async Task<bool> DeleteAsync(long id)
        {
            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            if (await _repository.IsAssignedToTeamAsync(id))
            {
                throw new InvalidOperationException(
                    "This salesman is assigned to a marketing team. " +
                    "Deactivate the salesman instead of deleting.");
            }

            await _repository.DeleteAsync(salesman);

            return true;
        }

        // Get active salesmen for dropdowns
        public async Task<List<SalesmanLookupDto>> GetActiveSalesmenAsync()
        {
            var salesmen = await _repository.GetAllAsync();

            return salesmen
                .Where(s => s.Status == (short)RecordStatus.Active)
                .Select(s => new SalesmanLookupDto
                {
                    Id = s.Id,
                    SalesmanCode = s.Salesmancode,
                    SalesmanName = s.Salesmanname
                })
                .ToList();
        }

        // Validate the joining date
        private static void ValidateJoiningDate(DateOnly? joiningDate)
        {
            if (joiningDate.HasValue &&
                joiningDate.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                throw new ArgumentException(
                    "Joining date cannot be in the future.");
            }
        }

        // Convert empty optional values to null
        private static string? CleanOptionalValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        // Convert database model to response DTO
        private static SalesmanResponseDto MapToResponse(Salesman salesman)
        {
            return new SalesmanResponseDto
            {
                Id = salesman.Id,
                SalesmanCode = salesman.Salesmancode,
                SalesmanName = salesman.Salesmanname,
                MobileNo = salesman.Mobileno,
                Email = salesman.Email,
                JoiningDate = salesman.Joiningdate,
                Status = salesman.Status,
                CreatedDate = salesman.Createddate,
                ModifiedDate = salesman.Modifieddate
            };
        }
    }
}
