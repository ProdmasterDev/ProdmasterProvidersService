using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Specification;
using System.Data;
using System.Diagnostics.Metrics;

namespace ProdmasterProvidersService.Services
{
    public class SpecificationService : ISpecificationService
    {
        private readonly SpecificationRepository _specificationRepository;
        public SpecificationService(SpecificationRepository specificationRepository)
        {
            _specificationRepository = specificationRepository;
        }        

        public Task<Specification> AddOrUpdateSpecification(User user, SpecificationModel specification, SpecificationSaveMode mode)
        {
            if (user.Products == null)
            {
                return null;
            }
            if (user.Specifications == null)
            {
                user.Specifications = new List<Specification>();
            }

            var productIds = specification.Products.Select(c => c.Id);

            var entity = user.Specifications.FirstOrDefault(c => c.Id == specification.Id) ?? new Specification { Id = default, UserId = user.Id };

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
                if (mode == SpecificationSaveMode.New)
                {
                    entity.VerifyState = VerifyState.NotSended;
                }
                else
                {
                    entity.VerifyState = VerifyState.Draft;
                }
            }
            
            entity.StartsAt = specification.StartsAt!.Value;
            entity.ExpiresAt = specification.ExpiresAt!.Value;
            entity.ProductSpecifications.RemoveAll(c => !productIds.Contains(c.ProductId));
            var products = user.Products
                .Where(c => productIds.Contains(c.Id))
                .ToList();

            foreach (var p in products)
            {
                var prodspec = entity.ProductSpecifications.FirstOrDefault(c => c.ProductId == p.Id);
                if (prodspec == null)
                {
                    prodspec = new ProductSpecification
                    {
                        Product = p,
                        Specification = entity,
                        Price = specification.Products.FirstOrDefault(x => x.Id == p.Id)?.Price ?? 0,
                    };
                    entity.ProductSpecifications.Add(prodspec);
                }
                else
                {
                    prodspec.Price = specification.Products.FirstOrDefault(x => x.Id == p.Id)?.Price ?? 0;
                }
            }
            return _specificationRepository.Update(entity);
        }

        public async Task<SpecificationModel> GetSpecificationModel(User user, long id)
        {
            if (user.Specifications != null)
            {
                var specification = user.Specifications.FirstOrDefault(c => c.Id == id);
                if (specification == null) return null;                
                var model = new SpecificationModel
                {
                    Id = id,
                    StartsAt = specification.StartsAt,
                    ExpiresAt = specification.ExpiresAt,
                    Products = specification.ProductSpecifications.OrderBy(c => c.Product.Name)
                    .Select(c => new SpecificationProductModel
                    {
                        Id = c.ProductId,
                        Price = c.Price,
                        Name = c.Product.Name,
                    }).ToList(),
                    CreatedAt = specification.CreatedAt,
                    LastModified = specification.LastModified,
                    VerifyState = specification.VerifyState,
                    VerifyNote = specification.VerifyNote,
                };
                if (!model.Products.Any()) model.Products.Add(new SpecificationProductModel() { Id = default, Price = default });
                return model;

            }
            return null;
        }

        public Task<bool> DeleteSpecifications(User user, IEnumerable<long> idArray)
        {
            return _specificationRepository.RemoveRange(user.Id, idArray);
        }        
    }
}
