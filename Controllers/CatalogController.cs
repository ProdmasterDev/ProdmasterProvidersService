using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Catalog;
using System.Data;
using System.Linq;
using System.Reflection;

namespace ProdmasterProvidersService.Controllers
{
    [Authorize]
    [Route("catalog")]
    public class CatalogController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICatalogService _catalogService;
        private readonly IUserService _userService;
        private readonly ISpecificationService _specificationService;

        public CatalogController(ILogger<HomeController> logger, ICatalogService catalogService, IUserService userService, ISpecificationService specificationService)
        {
            _logger = logger;
            _catalogService = catalogService;
            _userService = userService;
            _specificationService = specificationService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index([FromQuery] long? id, string propertyNameForOrder = "Name", string orderDirection = "asc", string searchString = "")
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }

            var products = user.Products;
            searchString = searchString.ToLower().Trim();

            if (!string.IsNullOrEmpty(searchString))
            {
                if (searchString.Length > 1 && products != null && products.Count > 0)
                {
                    products = products
                        .Where(p =>
                           p.Name.Contains(searchString)
                        || p.Standart.Name.ToLower().Contains(searchString)
                        || (p.VendorCode != null && p.VendorCode.ToLower().Contains(searchString))
                        || (p.Manufacturer != null && p.Manufacturer.Name.ToLower().Contains(searchString))
                    ).ToList();
                }
            }

            await FillSelectsViewBag();
            if (user.Products != null && user.Products.Any())
            {
                if (id != null)
                {
                    return View("ViewProduct", await _catalogService.GetProductModel(user, id.Value));
                }
                
                return View(new CatalogModel { Products = SortingHelper.OrderByDynamic(user.Products, propertyNameForOrder, orderDirection).ToList() });
            }
            return View(new CatalogModel { Products = new List<Product>() });
        }

        [HttpPost]
        [Route("deleteProducts")]
        public async Task<IActionResult> DeleteProducts([FromBody] IEnumerable<long> idArray)
        {
            var user = await GetUser();
            if (await _catalogService.DeleteProducts(user, idArray)) return Ok();
            else return BadRequest("Ошибка удаления");
        }

        [HttpGet]
        [Route("refreshProductsTable")]
        public async Task<IActionResult> RefreshProductsTable(string propertyNameForOrder = "Name", string orderDirection = "asc", string searchString = "")
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }

            var products = user.Products;
            searchString = searchString.ToLower().Trim();

            if (!string.IsNullOrEmpty(searchString))
            {
                if(searchString.Length > 1 && products != null && products.Count > 0)
                {
                    products = products
                        .Where(p => 
                           p.Name.Contains(searchString) 
                        || p.Standart.Name.ToLower().Contains(searchString) 
                        || (p.VendorCode != null && p.VendorCode.ToLower().Contains(searchString))
                        || (p.Manufacturer != null && p.Manufacturer.Name.ToLower().Contains(searchString))
                    ).ToList();
                }
            }

            if (products != null && products.Any())
            {
                return PartialView("_ProductsTable", new CatalogModel { Products = SortingHelper.OrderByDynamic(products, propertyNameForOrder, orderDirection).ToList() });
            }
            return PartialView("_ProductsTable", new CatalogModel { Products = new List<Product>() });
        }

        [HttpGet]
        [Route("editProduct")]
        public async Task<IActionResult> EditProduct(int id, long standart)
        {

            var user = await GetUser();
            var model = id == default ? new ProductModel { CountryId = 600, WithEmptyManufacturer = false } : await _catalogService.GetProductModel(user, id);

            if (model.VerifyState == VerifyState.Sended || model.VerifyState == VerifyState.Verified)
            {
                return RedirectToAction("Index", "Catalog", new { id = id });
            }

            if (standart != default)
            {
                model.StandartId = standart;
            }

            await FillViewBag();
            return View(model);

        }

        [HttpPost]
        [Route("editProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductModel model, bool addPrice = false)
        {
            var user = await GetUser();
            if (ModelState.IsValid)
            {
                
                if (model.VerifyState == VerifyState.Sended || model.VerifyState == VerifyState.Verified)
                {
                    return RedirectToAction("Index", "Catalog", new { id = model.Id });
                }
                var product = await _catalogService.AddOrUpdateProduct(user, model);
                if (addPrice)
                {
                    var specificationId = user?.Specifications?
                        .Where(c => c.LastModified.Date == DateTime.Now.Date 
                            && !c.Products.Contains(product)
                            && (c.VerifyState == VerifyState.NotSended || c.VerifyState == VerifyState.Draft))
                        .OrderByDescending(s => s.LastModified)
                        .LastOrDefault()?.Id;
                    return RedirectToAction("EditSpecification", "Specification", new { id = specificationId, productToAdd = product.Id });
                }
                if (product != null)
                {
                    return RedirectToAction("Index", "Catalog");
                }
                ModelState.AddModelError("", "Произошла ошибка добавления товара, попробуйте позже");
            }
            await FillViewBag();
            return View(model);
        }

        [NonAction]
        private Task<User> GetUser()
        {
            var userName = User.Identity?.Name;
            return _userService.GetByEmail(userName);
        }

        [NonAction]
        private async Task FillViewBag()
        {
            var standarts = await _catalogService.GetStandarts();
            var countries = await _catalogService.GetCountries();
            var manufacturers = await _catalogService.GetManufacturers();
            var units = standarts.DistinctBy(c => c.UnitId).ToDictionary(c => c.UnitId!.Value, c => c.Unit);
            var standartUnits = standarts.ToDictionary(c => c.Id, c => c.UnitId!.Value);
            ViewBag.Units = JsonConvert.SerializeObject(units);
            ViewBag.StandartUnits = JsonConvert.SerializeObject(standartUnits);
            ViewBag.Stands = standarts;
            ViewBag.Countries = countries;
            ViewBag.Manufacturers = manufacturers;
        }

        [NonAction]
        private async Task FillSelectsViewBag()
        {
            var selectOrderPropertyDictionary = new Dictionary<string, string>()
            {
                {"Name", "Название"},
                {"VendorCode", "Артикул" },
                {"CreatedAt", "Дата создания"},
                {"LastModified", "Дата последнего изменения"},
            };
            var selectDirectionDictionary = new Dictionary<string, string>()
            {
                {"asc", "По возрастанию"},
                {"desc", "По убыванию"},
            };

            ViewBag.OrderProperty = selectOrderPropertyDictionary;
            ViewBag.Direction = selectDirectionDictionary;
        }
    }
}
