namespace SalesMarketing.DTOs
{
    public class SalesmanResponseDto
    {
        public long Id { get; set; }

        public string SalesmanCode { get; set; } = string.Empty;

        public string SalesmanName { get; set; } = string.Empty;

        public string? MobileNo { get; set; }

        public string? Email { get; set; }

        public DateOnly? JoiningDate { get; set; }

        public short Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
