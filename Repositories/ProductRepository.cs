using ProdmasterProvidersService.Database;
using System.Linq.Expressions;
using System;
using ProvidersDomain.Models;

namespace ProdmasterProvidersService.Repositories
{
    public class ProductRepository : Repository<Product, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<Product, long>> Key => model => model.Id;
        public ProductRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Products)
        {
            _dbContext = dbContext;
        }
        public async Task<bool> RemoveRange(long userId, IEnumerable<long> idArray)
        {
            try
            {
                var entities = _dBSet.Where(c => c.UserId == userId && c.VerifyState == VerifyState.NotSended && idArray.Contains(c.Id)).ToList();
                _dBSet.RemoveRange(entities);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
