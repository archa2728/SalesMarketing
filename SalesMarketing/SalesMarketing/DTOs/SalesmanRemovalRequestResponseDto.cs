namespace SalesMarketing.DTOs
{
    public class SalesmanRemovalRequestResponseDto
    {
        public long Id { get; set; }

        public long SalesmanId { get; set; }
        public string SalesmanCode { get; set; } = string.Empty;
        public string SalesmanName { get; set; } = string.Empty;

        public long MarketingTeamId { get; set; }
        public string TeamCode { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;

        public long RequestedBy { get; set; }
        public string RequestedByUsername { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }

        // 1 = Pending, 2 = Approved, 3 = Rejected
        public short Status { get; set; }

        public long? ReviewedBy { get; set; }
        public string? ReviewedByUsername { get; set; }
        public DateTime? ReviewedDate { get; set; }

        public string? RejectionReason { get; set; }
    }
}
