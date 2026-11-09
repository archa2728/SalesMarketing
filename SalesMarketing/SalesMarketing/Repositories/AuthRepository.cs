using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.Models;
using SalesMarketing.Repositories.Interfaces;

namespace SalesMarketing.Repositories
{
    public class AuthRepository: IAuthRepository
    {
        private readonly SalesMarketingDbContext _context;

        public AuthRepository(SalesMarketingDbContext context)
        {
            _context = context;
        }

        public async Task<Appuser?> GetUserByUsernameAsync(string username)
        {
            return await _context.Appusers
                .FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
