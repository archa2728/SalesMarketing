using SalesMarketing.Models;

namespace SalesMarketing.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<Appuser?> GetUserByUsernameAsync(string username);
    }
}
