using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class UpdateSalesmanDto
    {
        [Required(ErrorMessage = "Salesman name is required.")]
        [StringLength(100, ErrorMessage = "Salesman name cannot exceed 100 characters.")]
        public string SalesmanName { get; set; } = string.Empty;

        [RegularExpression(@"^\d{7,15}$",
            ErrorMessage = "Mobile number must contain 7 to 15 digits.")]
        public string? MobileNo { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string? Email { get; set; }

        public DateOnly? JoiningDate { get; set; }

        [Range(1, 2, ErrorMessage = "Status must be 1 (Active) or 2 (Inactive).")]
        public short Status { get; set; } = 1;
    }
}
