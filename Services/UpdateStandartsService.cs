using Newtonsoft.Json;
using ProdmasterProvidersService.Database;
using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using System.Net.Http.Headers;
using System.Text;

namespace ProdmasterProvidersService.Services
{
    public class UpdateStandartsService : IUpdateStandartsService
    {
        private readonly HttpClient _httpClient;
        private readonly StandartRepository _standartRepository;
        private const string url = "http://192.168.1.251:8444/api";

        public UpdateStandartsService(HttpClient httpClient, StandartRepository repository)
        {
            _httpClient = httpClient;
            _standartRepository = repository;
        }
        public async Task Update()
        {
            var units = await GetUnits();
            if (units != null)
            {
                await UpdateUnits(units);
            }

            var standarts = await GetStandarts();
            if (standarts != null)
            {
                await UpdateStandarts(standarts);
            }

            var countries = await GetCountries();
            if (countries != null)
            {
                await UpdateCountries(countries);
            }

            var manufacturers = await GetManufacturers();
            if (manufacturers != null)
            {
                await UpdateManufacturers(manufacturers);
            }
        }

        private T? GetObjectFromDataString<T>(string dataString)
        {
            return JsonConvert.DeserializeObject<T>(dataString);
        }

        private async Task<T?> GetObjectsFromQueryAsync<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                var content = new StringContent(query, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                var result = await _httpClient.PostAsync(url, content);
                if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var dataString = await result.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(dataString))
                    {
                        return GetObjectFromDataString<T>(dataString);
                    }
                }
            }
            return default;
        }

        private Task UpdateStandarts(IEnumerable<Standart> standarts)
        {
            if (standarts.Any())
            {
                standarts = standarts.Where(c => c.UnitId != null && c.UnitId != 0);
                foreach (var standart in standarts)
                {
                    standart.Name = standart.Name.Trim();
                    standart.CategoryName = standart.CategoryName?.Trim() ?? "Остальное";
                    standart.Taste = standart.Taste?.Trim() ?? "Не указан";
                    standart.Smell = standart.Smell?.Trim() ?? "Не указан";
                    standart.Color = standart.Color?.Trim() ?? "Не указан";
                    standart.Structure = standart.Structure?.Trim() ?? "Не указана";
                    standart.Consistenc = standart.Consistenc?.Trim() ?? "Не указана";
                }
                return _standartRepository.AddOrUpdateRange(standarts);
            }
            return Task.CompletedTask;
        }

        private Task UpdateUnits(IEnumerable<Unit> units)
        {
            if (units.Any())
            {
                units = units.DistinctBy(c => c.Id).Where(c => c.Id != default);
                foreach (var unit in units)
                {
                    unit.Name = unit.Name.Trim();
                    unit.Short = unit.Short.Trim();
                }
                return _standartRepository.AddOrUpdateRange(units);
            }
            return Task.CompletedTask;
        }

        private Task UpdateCountries(IEnumerable<Country> countries)
        {
            if (countries.Any())
            {
                countries = countries.DistinctBy(c => c.Id);
                foreach (var unit in countries)
                {
                    unit.Name = unit.Name?.Trim();
                    unit.FullName = unit.FullName?.Trim();
                    unit.EngName = unit.EngName?.Trim();
                    unit.Code = unit.Code?.Trim();
                    unit.Code3 = unit.Code3?.Trim();
                }
                return _standartRepository.AddOrUpdateRange(countries);
            }
            return Task.CompletedTask;
        }

        private Task UpdateManufacturers(IEnumerable<Manufacturer> manufacturers)
        {
            if (manufacturers.Any())
            {
                manufacturers = manufacturers.DistinctBy(c => c.Id);
                foreach (var unit in manufacturers)
                {
                    unit.Name = unit.Name?.Trim();
                }
                return _standartRepository.AddOrUpdateRange(manufacturers);
            }
            return Task.CompletedTask;
        }

        private Task<IEnumerable<Standart>?> GetStandarts()
        {
            var query = "\"select stands.number, stands.stock, stands.taste, stands.smell, stands.color, stands.structure, stands.consistenc, iif(not(isnull(saname.cvalue) or empty(saname.cvalue)), saname.cvalue, iif(not(empty(stands.nameprod)),stands.nameprod,stock.name)) as nameprod, stands.create, stands.modify, stock.unit as unitId, unit.short as unitShort, unit.name as unitName, sagroup.cvalue as categoryname " +
                "from standart.dbf as stands " +
                "left join stock.dbf as stock on stock.number == stands.stock " +
                "left join unitn.dbf as unit on unit.number == stock.unit " +
                "left join stockattribute.dbf as sagroup on stock.number == sagroup.r1 and sagroup.name == 'ГРУППА ПОРТАЛ' " +
                "left join stockattribute.dbf as saname on stock.number == saname.r1 and saname.name == 'НАИМЕНОВАНИЕ ПОРТАЛ' " +
                "where stands.custom == 751601 and not stands.arch\"";
            return GetObjectsFromQueryAsync<IEnumerable<Standart>>(query);
        }
        private Task<IEnumerable<Unit>?> GetUnits()
        {
            var query = "\"select unit.number as unitId, unit.short as unitShort, unit.name as unitName from unitn as unit\"";
            return GetObjectsFromQueryAsync<IEnumerable<Unit>>(query);
        }
        private Task<IEnumerable<Country>?> GetCountries()
        {
            var query = "\"select number, name, fullname, engname, code, code3 from country\"";
            return GetObjectsFromQueryAsync<IEnumerable<Country>>(query);
        }
        private Task<IEnumerable<Manufacturer>?> GetManufacturers()
        {
            var query = "\"select number, name, create, modify from manufacturer where not arch\"";
            return GetObjectsFromQueryAsync<IEnumerable<Manufacturer>>(query);
        }
    }
}
