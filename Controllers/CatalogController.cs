using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Catalog;
using System.Data;

namespace ProdmasterProvidersService.Controllers
{
    [Authorize]
    [Route("catalog")]
    public class CatalogController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICatalogService _catalogService;
        private readonly IUserService _userService;

        public CatalogController(ILogger<HomeController> logger, ICatalogService catalogService, IUserService userService)
        {
            _logger = logger;
            _catalogService = catalogService;
            _userService = userService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index([FromQuery] long? id)
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }
            if (user.Products != null && user.Products.Any())
            {
                if (id != null)
                {
                    return View("ViewProduct", await _catalogService.GetProductModel(user, id.Value));
                }

                return View(new CatalogModel { Products = user.Products.OrderBy(c => c.Name).ToList() });
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
        public async Task<IActionResult> RefreshProductsTable()
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }
            if (user.Products != null && user.Products.Any())
            {

                return PartialView("_ProductsTable", new CatalogModel { Products = user.Products.OrderBy(c => c.Name).ToList() });
            }
            return PartialView("_ProductsTable", new CatalogModel { Products = new List<Product>() });
        }

        [HttpGet]
        [Route("editProduct")]
        public async Task<IActionResult> EditProduct(int id, long standart)
        {

            var user = await GetUser();
            var model = id == default ? new ProductModel { CountryId = 600 } : await _catalogService.GetProductModel(user, id);

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
        public async Task<IActionResult> EditProduct(ProductModel model)
        {
            var user = await GetUser();
            if (ModelState.IsValid)
            {
                if (model.VerifyState == VerifyState.Sended || model.VerifyState == VerifyState.Verified)
                {
                    return RedirectToAction("Index", "Catalog", new { id = model.Id });
                }
                var product = await _catalogService.AddOrUpdateProduct(user, model);
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
    }
}
