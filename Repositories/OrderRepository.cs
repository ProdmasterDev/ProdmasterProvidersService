using ProdmasterProvidersService.Database;
using ProvidersDomain.Models;
using System.Linq.Expressions;

namespace ProdmasterProvidersService.Repositories
{
    public class OrderRepository : Repository<Order, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<Order, long>> Key => model => model.Id;
        public OrderRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Orders)
        {
            _dbContext = dbContext;
        }
    }
}
