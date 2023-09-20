using ProdmasterProvidersService.Database;
using System.Linq.Expressions;
using System;
using ProvidersDomain.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Collections.Generic;

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

        public new async Task AddOrUpdateRange(IEnumerable<Specification> range)
        {
            try
            {
                var bulkConfig = new BulkConfig
                {
                    SetOutputIdentity = true,
                    BatchSize = 4000,
                    UpdateByProperties = new List<string> { nameof(Specification.DisanId) },
                    PropertiesToExclude = new List<string> { nameof(Specification.Id) },
                    //PropertiesToInclude = new List<string> { nameof(Specification.DisanId) }
                };
                await _dbContext.BulkInsertOrUpdateAsync(range, bulkConfig);
                await _dbContext.BulkSaveChangesAsync();
            }
            catch (DbUpdateException exception) {}
            catch (Exception exception) { var a = exception.Message; }
        }
    }
}
