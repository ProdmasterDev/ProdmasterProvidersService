using ProdmasterProvidersService.Database;
using System.Linq.Expressions;
using System;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using ProvidersDomain.Models;

namespace ProdmasterProvidersService.Repositories
{
    public class StandartRepository : Repository<Standart, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<Standart, long>> Key => model => model.Id;
        public StandartRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Standarts)
        {
            _dbContext = dbContext;
        }
        public Task<Standart[]> GetAllStandarts()
        {
            return _dBSet.OrderBy(c => c.CategoryName)                
                .ThenBy(c => c.Name)
                .ToArrayAsync();
        }
        public async Task AddOrUpdateRange<TModel>(IEnumerable<TModel> range) where TModel : class
        {
            try
            {
                var bulkConfig = new BulkConfig { SetOutputIdentity = true, BatchSize = 4000 };
                await _dbContext.BulkInsertOrUpdateAsync(range, bulkConfig);
                await _dbContext.BulkSaveChangesAsync();
            }
            catch (DbUpdateException exception) { }
        }
    }
}
