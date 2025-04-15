using Dapper;
using System.Data;
using WatchParty.WS.Entities;

namespace WatchParty.WS.Repositories.UserRepository
{
    public class UserRepository(IDbConnection db) : IUserRepository
    {
        private readonly IDbConnection _db = db;

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var sql = "SELECT * FROM public.user_details WHERE id = @Id";
            return await _db.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }
    }
}
