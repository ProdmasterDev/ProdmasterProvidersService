using ProdmasterProvidersService.Database;
using System.Linq.Expressions;
using System;
using ProvidersDomain.Models;

namespace ProdmasterProvidersService.Repositories
{
    public class SpecificationRepository : Repository<Specification, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<Specification, long>> Key => model => model.Id;
        public SpecificationRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Specifications)
        {
            _dbContext = dbContext;
        }
        public async Task<bool> RemoveRange(long userId, IEnumerable<long> idArray)
        {
            try
            {
                var entities = _dBSet.Where(c => c.UserId == userId && idArray.Contains(c.Id) && (c.VerifyState == VerifyState.Draft || c.VerifyState == VerifyState.NotSended || c.VerifyState == VerifyState.NotVerified || c.VerifyState == VerifyState.Corrected)).ToList();
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
