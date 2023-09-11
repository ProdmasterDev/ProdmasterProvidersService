using ProdmasterProvidersService.Database;
using System.Linq.Expressions;
using System;
using ProvidersDomain.Models;

namespace ProdmasterProvidersService.Repositories
{
    public class UserRepository : Repository<User, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<User, long>> Key => model => model.Id;
        public UserRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Users)
        {
            _dbContext = dbContext;
        }
    }
}
