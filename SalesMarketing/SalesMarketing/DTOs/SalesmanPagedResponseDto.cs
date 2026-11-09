namespace SalesMarketing.DTOs
{
    public class SalesmanPagedResponseDto
    {
        public List<SalesmanResponseDto> Items { get; set; } = new();

        public int TotalCount { get; set; }
    }
}
