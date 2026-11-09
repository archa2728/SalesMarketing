namespace SalesMarketing.DTOs
{
    public class LoginResponseDto
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public short Adminrole { get; set; }

        public string Token { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}