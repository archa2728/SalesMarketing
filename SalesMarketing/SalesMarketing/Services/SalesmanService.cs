
using SalesMarketing.DTOs;
using SalesMarketing.Enums;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services.Interfaces;
using SalesMarketing.Exceptions;

namespace SalesMarketing.Services
{
    public class SalesmanService : ISalesmanService
    {
        private readonly ISalesmanRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public SalesmanService(
            ISalesmanRepository repository,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
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
                throw new ConflictException(
                    "A salesman with this code already exists.");
            }

            string? email = NormalizeEmail(dto.Email);
            string? mobileNo = CleanOptionalValue(dto.MobileNo);

            if (email != null &&
                await _repository.EmailExistsAsync(email))
            {
                throw new ConflictException(
                    "A salesman with this email address already exists.");
            }

            if (mobileNo != null &&
                await _repository.MobileExistsAsync(mobileNo))
            {
                throw new ConflictException(
                    "A salesman with this mobile number already exists.");
            }

            ValidateJoiningDate(dto.JoiningDate);

            var salesman = new Salesman
            {
                Salesmancode = code,
                Salesmanname = dto.SalesmanName.Trim(),
                Mobileno = mobileNo,
                Email = email,
                Joiningdate = dto.JoiningDate,
                Status = dto.Status
            };

            await _repository.AddAsync(salesman);

            // Audit log: Create
            await _auditLogService.WriteAsync(
                action: "Create",
                entityName: "Salesman",
                entityId: salesman.Id,
                newValues: new
                {
                    salesman.Salesmancode,
                    salesman.Salesmanname,
                    salesman.Mobileno,
                    salesman.Email,
                    salesman.Joiningdate,
                    salesman.Status
                });

            return MapToResponse(salesman);
        }

        // Update salesman details
        public async Task<bool> UpdateAsync(
            long id,
            UpdateSalesmanDto dto)
        {
            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            // Prevent direct deactivation if salesman belongs
            // to an active marketing team.
            if (dto.Status == (short)RecordStatus.Inactive &&
                salesman.Status != (short)RecordStatus.Inactive)
            {
                bool hasActiveMembership =
                    await _repository.HasActiveMembershipInActiveTeamAsync(id);

                if (hasActiveMembership)
                {
                    throw new ConflictException(
                        "This salesman belongs to an active marketing team. Submit a removal request instead.");
                }
            }

            string? email = NormalizeEmail(dto.Email);
            string? mobileNo = CleanOptionalValue(dto.MobileNo);

            if (email != null &&
                await _repository.EmailExistsForOtherAsync(email, id))
            {
                throw new ConflictException(
                    "A salesman with this email address already exists.");
            }

            if (mobileNo != null &&
                await _repository.MobileExistsForOtherAsync(mobileNo, id))
            {
                throw new ConflictException(
                    "A salesman with this mobile number already exists.");
            }

            ValidateJoiningDate(dto.JoiningDate);

            // Capture old values before updating
            var oldValues = new
            {
                salesman.Salesmanname,
                salesman.Mobileno,
                salesman.Email,
                salesman.Joiningdate,
                salesman.Status
            };

            short oldStatus = salesman.Status;

            salesman.Salesmanname = dto.SalesmanName.Trim();
            salesman.Mobileno = mobileNo;
            salesman.Email = email;
            salesman.Joiningdate = dto.JoiningDate;
            salesman.Status = dto.Status;
            salesman.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(salesman);

            // Capture new values after updating
            var newValues = new
            {
                salesman.Salesmanname,
                salesman.Mobileno,
                salesman.Email,
                salesman.Joiningdate,
                salesman.Status
            };

            // Audit log: Update or StatusChange
            await _auditLogService.WriteAsync(
                action: oldStatus != salesman.Status
                    ? "StatusChange"
                    : "Update",
                entityName: "Salesman",
                entityId: salesman.Id,
                oldValues: oldValues,
                newValues: newValues);

            return true;
        }

        // Activate or deactivate a salesman
        public async Task<bool> UpdateStatusAsync(
            long id,
            UpdateSalesmanStatusDto dto)
        {
            if (dto.Status != (short)RecordStatus.Active &&
                dto.Status != (short)RecordStatus.Inactive)
            {
                throw new ArgumentException("Invalid salesman status.");
            }

            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            // Prevent direct deactivation if salesman belongs
            // to an active marketing team.
            if (dto.Status == (short)RecordStatus.Inactive &&
                salesman.Status != (short)RecordStatus.Inactive)
            {
                bool hasActiveMembership =
                    await _repository.HasActiveMembershipInActiveTeamAsync(id);

                if (hasActiveMembership)
                {
                    throw new ConflictException(
                        "This salesman belongs to an active marketing team. Submit a removal request instead.");
                }
            }

            // Capture old status
            short oldStatus = salesman.Status;

            salesman.Status = dto.Status;
            salesman.Modifieddate = DateTime.UtcNow;

            await _repository.UpdateAsync(salesman);

            // Audit log: StatusChange
            await _auditLogService.WriteAsync(
                action: "StatusChange",
                entityName: "Salesman",
                entityId: salesman.Id,
                oldValues: new
                {
                    Status = oldStatus
                },
                newValues: new
                {
                    Status = salesman.Status
                });

            return true;
        }

        // Delete a salesman if not assigned to an active team
        public async Task<bool> DeleteAsync(long id)
        {
            var salesman = await _repository.GetByIdAsync(id);

            if (salesman == null)
                return false;

            // Block deletion only if assigned to an active team.
            if (await _repository.HasActiveMembershipInActiveTeamAsync(id))
            {
                throw new ConflictException(
                    "This salesman belongs to an active marketing team. " +
                    "Submit a removal request instead.");
            }

            // Capture old values before archiving
            var oldValues = new
            {
                salesman.Status,
                salesman.Isarchived
            };

            await _repository.DeleteAsync(salesman);

            // Audit log: Archive
            await _auditLogService.WriteAsync(
                action: "Archive",
                entityName: "Salesman",
                entityId: salesman.Id,
                oldValues: oldValues,
                newValues: new
                {
                    Status = (short)RecordStatus.Inactive,
                    Isarchived = true
                });

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

        private static string? NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim().ToLowerInvariant();
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
