using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Home;

namespace ProdmasterProvidersService.Services
{
    public class HomeService : IHomeService
    {
        private readonly StandartRepository _standartRepository;
        public HomeService(StandartRepository standartRepository)
        {
            _standartRepository = standartRepository;
        }

        public Task<Standart> GetStandart(long id)
        {
            return _standartRepository.First(c => c.Id == id);
        }

        public async Task<StandartListModel> GetStandartListModel()
        {
            var standarts = await _standartRepository.Select();
            var standartModelList = standarts.Select(c => new StandartModel
            {
                Id = c.Id,
                Name = c.Name,
                CategoryName = c.CategoryName ?? "Остальное",
                UnitName = c.Unit?.Name ?? string.Empty,
            }).OrderBy(c => c.CategoryName)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.UnitName)
            .ToList();

            return new StandartListModel
            {
                Standarts = standartModelList.GroupBy(c => c.CategoryName).ToList(),
            };
        }

        public async Task<StandartListModel> GetStandartListModel(string searchString)
        {
            //var standarts = (await _standartRepository.Where(c => c.Name.ToLower().Contains(searchString.ToLower()))).ToList();
            var words = searchString.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var allStandarts = await _standartRepository.Select();
            var standarts = new List<Standart>();

            if (allStandarts.Any())
            {
                foreach (var standart in allStandarts)
                {
                    if (words.All(w => standart.Name.ToLower().Contains(w))) 
                    {
                        standarts.Add(standart);
                    }
                }
            }

            var standartModelList = standarts.Select(c => new StandartModel
            {
                Id = c.Id,
                Name = c.Name,
                CategoryName = c.CategoryName ?? "Остальное",
                UnitName = c.Unit?.Name ?? string.Empty,
            }).OrderBy(c => c.CategoryName)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.UnitName)
            .ToList();

            return new StandartListModel
            {
                Standarts = standartModelList.GroupBy(c => c.CategoryName).ToList(),
                CollapseCategories = false,
            };
        }
    }
}
