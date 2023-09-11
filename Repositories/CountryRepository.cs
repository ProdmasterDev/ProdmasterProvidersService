using Microsoft.EntityFrameworkCore;
using ProdmasterProvidersService.Database;
using ProvidersDomain.Models;

namespace ProdmasterProvidersService.Repositories
{
    public class CountryRepository
    {
        private readonly UserContext _context;
        public CountryRepository(UserContext context)
        {
            _context = context;
        }
        public Task<Country[]> GetAll()
        {
            return _context.Countries.ToArrayAsync();
        }
    }
}
