//using System.ComponentModel.DataAnnotations;

//namespace SalesMarketing.DTOs
//{
//    public class CreateSalesmanDto
//    {
//        [Required(ErrorMessage = "Salesman code is required.")]
//        [StringLength(20, ErrorMessage = "Salesman code cannot exceed 20 characters.")]
//        [RegularExpression(@"^[a-zA-Z0-9]+$",
//            ErrorMessage = "Salesman code must contain only letters and numbers.")]
//        public string SalesmanCode { get; set; } = string.Empty;

//        [Required(ErrorMessage = "Salesman name is required.")]
//        [StringLength(100, ErrorMessage = "Salesman name cannot exceed 100 characters.")]
//        public string SalesmanName { get; set; } = string.Empty;

//        [RegularExpression(@"^\d{7,15}$",
//            ErrorMessage = "Mobile number must contain 7 to 15 digits.")]
//        public string? MobileNo { get; set; }

//        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
//        [StringLength(100)]
//        public string? Email { get; set; }

//        public DateOnly? JoiningDate { get; set; }

//        public short Status { get; set; } = 1;
//    }
//}


using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class CreateSalesmanDto
    {
        [Required(ErrorMessage = "Salesman code is required.")]
        [StringLength(20, ErrorMessage = "Salesman code cannot exceed 20 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$",
            ErrorMessage = "Salesman code must contain only letters and numbers.")]
        public string SalesmanCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Salesman name is required.")]
        [StringLength(100, ErrorMessage = "Salesman name cannot exceed 100 characters.")]
        [RegularExpression(@"^[\p{L}][\p{L}\s.'-]*$",
            ErrorMessage = "Salesman name can contain only letters, spaces, dots, apostrophes and hyphens.")]
        public string SalesmanName { get; set; } = string.Empty;

        [RegularExpression(@"^\d{7,15}$",
            ErrorMessage = "Mobile number must contain 7 to 15 digits.")]
        public string? MobileNo { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string? Email { get; set; }

        public DateOnly? JoiningDate { get; set; }

        [Range(1, 2, ErrorMessage = "Status must be either Active (1) or Inactive (2).")]
        public short Status { get; set; } = 1;
    }
}

