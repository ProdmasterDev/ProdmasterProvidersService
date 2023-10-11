using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Models.ApiModels;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Specification;
using System.Diagnostics.Metrics;

namespace ProdmasterProvidersService.Services
{
    public class SpecificationApiService : ISpecificationApiService
    {
        private readonly SpecificationRepository _specificationRepository;
        private readonly ProductRepository _productRepository;
        private readonly UserRepository _userRepository;
        public SpecificationApiService(SpecificationRepository specificationRepository, ProductRepository productRepository, UserRepository userRepository)
        {
            _specificationRepository = specificationRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<SpecificationApiModel>> GetNewSpecifications()
        {

            var specifications = await _specificationRepository.Select(c => c.VerifyState == VerifyState.NotSended || c.VerifyState == VerifyState.Corrected);
            return specifications.Select(c => new SpecificationApiModel
            {
                Id = c.Id,
                UserId = c.UserId,
                CustomId = c.User.DisanId,
                ProvidersName = c.User.Name,
                INN = c.User.INN,
                Email = c.User.Email,
                Phone = c.User.Phone,
                Products = c.Products.Select(p => new ProductApiModel
                {
                    Id = p.Id,
                    DisanId = p.DisanId,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    Price = p.ProductSpecifications.First(s => s.SpecificationId == c.Id).Price,
                    ManufacturerId = p.ManufacturerId,
                    ManufacturerName = p.ManufacturerName,
                    StandartId = p.StandartId,
                    CountryId = p.CountryId,
                    CreatedAt = p.CreatedAt,
                    LastModified = p.LastModified,
                    Brand = p.Brand,
                    Note = p.Note,
                    VendorCode = p.VendorCode
                }).ToList(),
                CreatedAt = c.CreatedAt,
                LastModified = c.LastModified,
                StartsAt = c.StartsAt,
                ExpiresAt = c.ExpiresAt,
            });
        }

        private Task<Specification[]> GetSpecifications(IEnumerable<long> specificationIds)
        {            
            return _specificationRepository.Select(c => specificationIds.Contains(c.Id));
        }
        private Task UpdateSpecificationStatus(IEnumerable<Specification> specifications, VerifyState state)
        {
            foreach (var specification in specifications)
            {
                specification.VerifyState = state;
                foreach (var product in specification.Products)
                {
                    if (product.VerifyState != VerifyState.Verified) 
                    {
                        product.VerifyState = state;
                    }
                }
            }
            return _specificationRepository.AddOrUpdateRange(specifications);
        }

        private Task UpdateSpecificationsDisanId(IEnumerable<Specification> specifications, VerifyState state)
        {
            foreach (var specification in specifications)
            {
                specification.VerifyState = state;
                foreach (var product in specification.Products)
                {
                    if (product.VerifyState != VerifyState.Verified)
                    {
                        product.VerifyState = state;
                    }
                }
            }
            return _specificationRepository.AddOrUpdateRange(specifications);
        }

        public async Task AddOrUpdateSpecifications(IEnumerable<UpdateSpecificationApiModel> specifications)
        {
            foreach (var specification in specifications)
            {
                var dbSpecification = await _specificationRepository.First(c => c.Id == specification.Id);
                dbSpecification.VerifyState = specification.Verified ? VerifyState.Verified : VerifyState.NotVerified;
                dbSpecification.DisanId = specification.DisanId;
                dbSpecification.VerifyNote = specification.VerifyNote;
                await _specificationRepository.Update(dbSpecification);
                foreach (var product in specification.Products)
                {
                    var dbProduct = await _productRepository.First(c => c.Id == product.Id);
                    dbProduct.ManufacturerId = product.ManufacturerId;
                    dbProduct.DisanId = product.DisanId;
                    dbProduct.VerifyState = product.Verified ? VerifyState.Verified : VerifyState.NotVerified;                    
                    dbProduct.VerifyNote = product.VerifyNote;
                    var dbSpecProdIndex = dbProduct.ProductSpecifications.FindIndex(c => c.SpecificationId == dbSpecification.Id);
                    dbProduct.ProductSpecifications[dbSpecProdIndex].Price = product.Price;                    

                    await _productRepository.Update(dbProduct);
                }
            }
        }

        public async Task<bool> SuccessSendingSpecifications(IEnumerable<long> specificationIds)
        {
            var specifications = await GetSpecifications(specificationIds);
            try
            {
                await UpdateSpecificationStatus(specifications, VerifyState.Verified);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ConfirmSendingSpecifications(IEnumerable<ConfirmSpecificationApiModel> specifications)
        {
            foreach (var specification in specifications)
            {
                if (!specification.IsVerified) { continue; }
                var specificationToUpdate = await _specificationRepository.First(s => s.Id == specification.Id);
                if (specificationToUpdate != null)
                {
                    var user = await _userRepository.First(u => u.Id == specificationToUpdate.UserId);
                    if (user != null)
                    {
                        if(user.DisanId == default && specification.CustomId != default)
                        {
                            user.DisanId = specification.CustomId;
                            await _userRepository.Update(user);
                        }
                    }
                    specificationToUpdate.DisanId = specification.DisanId;
                    specificationToUpdate.VerifyState = VerifyState.Verified;
                    await _specificationRepository.Update(specificationToUpdate);
                    if (specification.Products != null)
                    {
                        foreach (var product in specification.Products)
                        {
                            if (!product.IsVerified) { continue; }
                            var productToUpdate = await _productRepository.First(p => p.Id == product.Id);
                            if (productToUpdate != null)
                            {
                                productToUpdate.DisanId = product.DisanId;
                                productToUpdate.VerifyState = VerifyState.Verified;
                                await _productRepository.Update(productToUpdate);
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
