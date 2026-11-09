using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class RejectSalesmanRemovalRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [StringLength(500, MinimumLength = 1,
        ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
