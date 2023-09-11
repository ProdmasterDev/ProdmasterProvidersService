using Microsoft.EntityFrameworkCore;
using ProdmasterProvidersService.Database;
using ProvidersDomain.Models;
using System.Linq.Expressions;

namespace ProdmasterProvidersService.Repositories
{
    public class ManufacturerRepository : Repository<Manufacturer, long, UserContext>
    {
        private readonly UserContext _dbContext;
        protected override Expression<Func<Manufacturer, long>> Key => model => model.Id;
        public ManufacturerRepository(UserContext ctx, UserContext dbContext) : base(dbContext,
            (appDbContext) => appDbContext.Manufacturers)
        {
            _dbContext = dbContext;
        }
        
    }
}
