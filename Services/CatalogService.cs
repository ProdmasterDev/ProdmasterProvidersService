using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Catalog;
using System.Diagnostics.Metrics;

namespace ProdmasterProvidersService.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly StandartRepository _standartRepository;
        private readonly ProductRepository _productRepository;
        private readonly CountryRepository _countryRepository;
        private readonly ManufacturerRepository _manufacturerRepository;
        public CatalogService(StandartRepository standartRepository, ProductRepository productRepository, CountryRepository countryRepository, ManufacturerRepository manufacturerRepository)
        {
            _standartRepository = standartRepository;
            _productRepository = productRepository;
            _countryRepository = countryRepository;
            _manufacturerRepository = manufacturerRepository;
        }
        public async Task<ProductModel> GetProductModel(User user, long id)
        {
            if (user.Products != null)
            {
                var product = user.Products.FirstOrDefault(c => c.Id == id);
                if (product == null) return null;
                var price = "-";
                var lastSpecification = product.Specifications.OrderByDescending(c => c.StartsAt).FirstOrDefault();
                if (lastSpecification != default)
                {
                    price = product.ProductSpecifications.FirstOrDefault(c => c.SpecificationId == lastSpecification.Id)?.Price.ToString("#,##0.00") ?? "-";
                }
                return new ProductModel 
                { 
                    Id = id, 
                    Name = product.Name,
                    VendorCode = product.VendorCode,
                    Quantity = product.Quantity, 
                    StandartId = product.Standart.Id,
                    Standart = product.Standart,
                    WithEmptyManufacturer = (product.ManufacturerId == default) ? true : false,
                    ManufacturerId = product.ManufacturerId,
                    ManufacturerName = product.ManufacturerName,
                    Brand = product.Brand,
                    CountryId = product.CountryId,
                    Country = product.Country,
                    Note = product.Note,
                    VerifyState = product.VerifyState,
                    VerifyNote = product.VerifyNote,
                    SpecificationId = user?.Specifications?.Where(c => c.LastModified.Date == DateTime.Now.Date && !c.Products.Contains(product) && (c.VerifyState == VerifyState.NotSended || c.VerifyState == VerifyState.Draft)).OrderByDescending(s => s.LastModified).LastOrDefault()?.Id,
                    LastPrice = price,
                };
            }
            return null;
        }

        public async Task<Product> AddOrUpdateProduct(User user, ProductModel product)
        {
            if (user.Products == null)
            {
                user.Products = new List<Product>();
            }
            var entity = user.Products.FirstOrDefault(c => c.Id == product.Id);
            if (entity == null)
            {
                entity = new Product { Id = default, UserId = user.Id };
            }
            if (entity.VerifyState == VerifyState.Sended || entity.VerifyState == VerifyState.Verified)
            {
                return null;
            }
            if (entity.VerifyState == VerifyState.NotVerified || entity.VerifyState == VerifyState.Corrected)
            {
                entity.VerifyState = VerifyState.Corrected;
            }
            else
            {
                entity.VerifyState = VerifyState.NotSended;
            }
            entity.Name = product.Name;
            entity.VendorCode = product.VendorCode;
            entity.Quantity = product.Quantity.Value;
            entity.StandartId = product.StandartId.Value;
            if (product.WithEmptyManufacturer)
            {
                entity.ManufacturerId = default;
                entity.ManufacturerName = default;
            }
            else
            {
                entity.ManufacturerId = product.ManufacturerId;
                entity.ManufacturerName = product.ManufacturerName;
            }
            entity.Brand = product.Brand;
            entity.CountryId = product.CountryId;
            entity.Note = product.Note;
            return await _productRepository.Update(entity);
        }

        public async Task<IEnumerable<Standart>> GetStandarts()
        {
            return (await _standartRepository.GetAllStandarts()).OrderBy(c => c.CategoryName == "Остальное");            
        }
        public async Task<IEnumerable<Country>> GetCountries()
        {
            return (await _countryRepository.GetAll()).OrderBy(c => c.Name);
        }
        public async Task<IEnumerable<Manufacturer>> GetManufacturers()
        {
            return (await _manufacturerRepository.Select()).OrderBy(c => c.Name);
        }

        public async Task<bool> DeleteProducts(User user, IEnumerable<long> idArray)
        {
            return await _productRepository.RemoveRange(user.Id, idArray);
        }
    }
}
