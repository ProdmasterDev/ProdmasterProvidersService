using Newtonsoft.Json;
using ProdmasterProvidersService.Database;
using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Models.ApiModels;
using ProvidersDomain.Services;
using System.Net.Http.Headers;
using System.Text;

namespace ProdmasterProvidersService.Services
{
    public class UpdateProvidersService : IUpdateProvidersService
    {
        private readonly HttpClient _httpClient;
        private readonly UserRepository _userRepository;
        private readonly ProductRepository _productRepository;
        private readonly SpecificationRepository _specificationRepository;
        private const string url = "http://192.168.1.251:8444/api";

        public UpdateProvidersService(HttpClient httpClient, UserRepository userRepository, ProductRepository productRepository, SpecificationRepository specificationRepository)
        {
            _httpClient = httpClient;
            _userRepository = userRepository;
            _productRepository = productRepository;
            _specificationRepository = specificationRepository;
        }
        public async Task LoadProvider(long customId, string? password)
        {
            var provider = await GetProvider(customId);

            if (provider != null)
            {
                var dbProvider = await _userRepository.First(x => x.DisanId == provider.DisanId) ?? await _userRepository.First(x => x.INN == provider.INN);

                if (dbProvider != null) throw new Exception("User exsists");

                provider = await AddProvider(provider, password);

                var products = await GetProductsForProvider(customId);
                if (products != null)
                {
                    products.ForEach(c => c.UserId = provider.Id);
                    await AddProducts(products);
                }

                var specifications = await GetSpecificationsForProvider(customId, provider.Id);
                if (specifications != null)
                {
                    await UpdateSpecifications(specifications);
                }
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
        private async Task AddProducts(List<Product> products)
        {
            if (products.Any())
            {
                foreach (var product in products)
                {
                    if (product.CountryId == 0) product.CountryId = null;
                    if (product.ManufacturerId == 0) product.ManufacturerId = null;
                    product.Name = product.Name.Trim();
                    product.Note = product.Note?.Trim();
                    product.VendorCode = product.VendorCode?.Trim();
                    product.Brand = product.Brand?.Trim();
                    product.VerifyState = VerifyState.Verified;
                    var dbProduct = await _productRepository.First(c => c.DisanId == product.DisanId);
                    if (dbProduct == null)
                        await _productRepository.Add(product);
                }
            }
        }
        private async Task<User> AddProvider(User user, string? password)
        {
            user.INN = user.INN.Trim();
            user.Password = password ?? user.INN;
            user.Name = user.Name.Trim();
            user.Email = user.Email.Trim();
            user.Phone = user.Phone.Trim();
            var dbProvider = await _userRepository.First(c => c.INN == user.INN);
            if (dbProvider != null)
            {
                dbProvider.Email = user.Email;
                dbProvider.Phone = user.Phone;
                dbProvider.Password = user.Password;
                return await _userRepository.Update(user);
            }
            return await _userRepository.Add(user);
        }
        private async Task<User?> GetProvider(long customId)
        {
            var query = "\"select alltrim(cusp.name) + alltrim(cus.c_pref) + alltrim(cus.name) + alltrim(cus.c_suff) as name, " +
                "IIF(LEN(ALLTRIM(inn)) == 12, ALLTRIM(inn), IIF(EMPTY(inn), SPACE(12), STREXTRACT(cus.inn, '',';'))) as inn, " +
                "ALLTRIM(cf.tel) as phone, " +
                "ALLTRIM(STREXTRACT(cf.email + ';', '', ';')) as email, " +
                "cus.create, cus.modify, cus.number " +
                "from custom as cus " +
                "inner join customp as cusp on cusp.number == cus.pref " +
                "inner join customf as cf ON cus.Number == cf.Number " +
                $"where cus.number == {customId}\"";
            return (await GetObjectsFromQueryAsync<IEnumerable<User>>(query))?.FirstOrDefault();
        }
        private Task<List<Product>?> GetProductsForProvider(long customId)
        {
            var query = "\"select st1.number, IIF(EMPTY(st1.nameprod),stock.name,st1.nameprod) as nameprod, st1.price, st1.minkvant, st1.article, st1.create, st1.modify, st1.country, st1.brand, st1.manufact, st1.stock, st2.number as parent from standart as st1 " +
                "inner join standart as st2 on st1.stock == st2.stock " +
                "inner join stock as stock on st1.stock == stock.number " +
                $"where not st1.arch and st2.custom == 751601 and st1.custom == {customId}\"";
            return GetObjectsFromQueryAsync<List<Product>>(query);
        }
        private async Task<IEnumerable<Specification>?> GetSpecificationsForProvider(long customId, long providerId)
        {
            var query = "\"select jr.number as disanId, jr.saled as startsAt, jr.end as expiresAt, jr.create as createdAt, jr.modify as lastModified " +
                "from jr as jr " +
                "left join irfree as rf on rf.journal == jr.number " +
                $"where jr.object == {customId} and not jr.arch and (LOWER(ALLTRIM(jr.doc_number)) == 'спецификация' or LOWER(ALLTRIM(jr.doc_number)) == 'мониторинг') and rf.standart <> 0 " +
                "and not(isnull(jr.saled)) and not(empty(jr.saled)) and not(isnull(jr.end)) and not(empty(jr.end)) " +
                "group by jr.number, jr.saled, jr.end, jr.create, jr.modify\"";
            var specifications = await GetObjectsFromQueryAsync<IEnumerable<Specification>>(query);

            if (specifications == null) return null;

            specifications.ToList().ForEach(c => c.UserId = providerId);
            specifications = await AddSpecifications(specifications);

            query = "\"select jr.number as jr, rf.price, rf.standart " +
                "from jr as jr " +
                "inner join irfree as rf on rf.journal == jr.number " +
                $"where jr.object == {customId} and not jr.arch and (LOWER(ALLTRIM(jr.doc_number)) == 'спецификация' or LOWER(ALLTRIM(jr.doc_number)) == 'мониторинг') and rf.standart <> 0\"";               

            var productSpecificationsGroups = (await GetObjectsFromQueryAsync<IEnumerable<ProductSpecificationApiModel>>(query))?.DistinctBy(c => new{ c.StandartDisanId, c.SpecificationDisanId}).GroupBy(c => c.SpecificationDisanId);
            if (productSpecificationsGroups != null)
            {
                foreach (var productSpecificationsGroup in productSpecificationsGroups)
                {
                    var specification = specifications?.FirstOrDefault(c => c.DisanId == productSpecificationsGroup.Key);
                    if (specification != null)
                    {
                        foreach (var productSpecification in productSpecificationsGroup)
                        {
                            var product = await _productRepository.First(c => c.DisanId == productSpecification.StandartDisanId);
                            if (product != null)
                            {
                                var prodspec = new ProductSpecification
                                {
                                    ProductId = product.Id,
                                    SpecificationId = specification.Id,
                                    Price = productSpecification.Price,
                                };
                                specifications.Where(c => c.DisanId == productSpecificationsGroup.Key).ToList()
                                    .ForEach(c => c.ProductSpecifications.Add(prodspec));

                            }
                        }
                    }
                }
            }
            specifications.ToList().ForEach(c => c.VerifyState = VerifyState.Verified);
            return specifications;
        }
        private async Task<IEnumerable<Specification>> AddSpecifications(IEnumerable<Specification> specifications)
        {
            var specificationList = new List<Specification>();
            foreach (var specification in specifications)
            {
                if (specification != null)
                {
                    var dbSpec = await _specificationRepository.Add(specification);
                    if (dbSpec != null) specificationList.Add(dbSpec);
                }
            }
            return specificationList;
        }
        private async Task UpdateSpecifications(IEnumerable<Specification> specifications)
        {
            await _specificationRepository.AddOrUpdateRange(specifications);
        }
    }
}
