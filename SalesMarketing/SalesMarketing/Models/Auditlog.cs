namespace SalesMarketing.Models
{
    public class AuditLog
    {
        public long Id { get; set; }

        public long? Userid { get; set; }

        public string Action { get; set; } = null!;

        public string Entityname { get; set; } = null!;

        public long Entityid { get; set; }

        public string? Oldvalues { get; set; }

        public string? Newvalues { get; set; }

        public DateTime Createddate { get; set; }
    }
}