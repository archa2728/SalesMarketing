
namespace SalesMarketing.DTOs
{
    public class MarketingTeamResponseDto
    {
        public long Id { get; set; }

        public string TeamCode { get; set; } = string.Empty;

        public string TeamName { get; set; } = string.Empty;

        public long TeamLeaderId { get; set; }

        public string? TeamLeaderName { get; set; }

        public string? Description { get; set; }

        public short Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public decimal TotalTargetAmount { get; set; }

        public List<MarketingTeamMemberResponseDto> Members { get; set; } = new();
    }

    public class MarketingTeamMemberResponseDto
    {
        public long Id { get; set; }

        public long SalesmanId { get; set; }

        public string SalesmanCode { get; set; } = string.Empty;

        public string SalesmanName { get; set; } = string.Empty;

        public short Role { get; set; }

        public decimal TargetAmount { get; set; }

        public short Status { get; set; }
    }
}
