namespace SalesMarketing.DTOs
{
    public class MarketingTeamPageResponseDto
    {
        public List<MarketingTeamResponseDto> Items { get; set; } = new();

        public int TotalCount { get; set; }
    }
}