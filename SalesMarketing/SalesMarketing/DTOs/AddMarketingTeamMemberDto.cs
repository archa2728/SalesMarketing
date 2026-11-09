using System.ComponentModel.DataAnnotations;

namespace SalesMarketing.DTOs
{
    public class AddMarketingTeamMemberDto
    {
        [Range(1, long.MaxValue)]
        public long SalesmanId { get; set; }

        [Range(1, 4, ErrorMessage = "Role must be between 1 and 4.")]
        public short Role { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99",
            ErrorMessage = "Target amount cannot be negative.")]
        public decimal TargetAmount { get; set; }

        [Range(1, 2)]
        public short Status { get; set; } = 1;
    }
}
