using WatchParty.WS.Entities;

namespace WatchParty.WS.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
    }
}
