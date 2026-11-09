namespace SalesMarketing.Models
{
    public class Salesmanremovalrequest
    {
        public long Id { get; set; }

        public long Salesmanid { get; set; }

        public long Marketingteamid { get; set; }

        public long Requestedby { get; set; }

        public DateTime Requesteddate { get; set; }

        // 1 = Pending, 2 = Approved, 3 = Rejected
        public short Status { get; set; }

        public long? Reviewedby { get; set; }

        public DateTime? Revieweddate { get; set; }

        public string? Rejectionreason { get; set; }
    }
}
