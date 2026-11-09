
using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class UpdateMarketingTeamDto
    {
        [Required]
        [StringLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long TeamLeaderId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, 2)]
        public short Status { get; set; } = 1;

        [Required]
        [MinLength(1, ErrorMessage = "Add at least one salesman to the team.")]
        public List<MarketingTeamMemberDto> Members { get; set; } = new();
    }
}
