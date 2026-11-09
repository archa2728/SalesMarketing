using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class UpdateMarketingTeamStatusDto
    {
        [Required]
        [Range(1, 2, ErrorMessage = "Status must be 1 (Active) or 2 (Inactive).")]
        public short Status { get; set; }
    }
}
