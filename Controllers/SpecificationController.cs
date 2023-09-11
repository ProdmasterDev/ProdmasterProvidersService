using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProdmasterProvidersService.Services;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Catalog;
using ProvidersDomain.ViewModels.Specification;
using System;
using System.Data;

namespace ProdmasterProvidersService.Controllers
{
    [Authorize]
    [Route("specification")]
    public class SpecificationController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISpecificationService _specificationService;
        private readonly IUserService _userService;

        public SpecificationController(ILogger<HomeController> logger, ISpecificationService specificationService, IUserService userService)
        {
            _logger = logger;
            _specificationService = specificationService;
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
            if (user.Specifications != null && user.Specifications.Any())
            {
                if (id != null)
                {
                    return View("ViewSpecification", await _specificationService.GetSpecificationModel(user, id.Value));
                }

                return View(new SpecificationIndexModel { Specifications = user.Specifications.OrderByDescending(c => c.CreatedAt).ToList() });
            }
            return View(new SpecificationIndexModel { Specifications = new List<Specification>() });
        }

        [HttpPost]
        [Route("deleteSpecifications")]
        public async Task<IActionResult> DeleteSpecifications([FromBody] IEnumerable<long> idArray)
        {
            var user = await GetUser();
            if (await _specificationService.DeleteSpecifications(user, idArray)) return Ok();
            else return BadRequest("Ошибка удаления");
        }

        [HttpGet]
        [Route("refreshSpecificationsTable")]
        public async Task<IActionResult> RefreshSpecificationsTable()
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogWarning($"Failed to get user, userName: {User.Identity?.Name};");
                return BadRequest("Failed to get user");
            }
            if (user.Specifications != null && user.Specifications.Any())
            {
                return PartialView("_SpecificationsTable", new SpecificationIndexModel { Specifications = user.Specifications.OrderByDescending(c => c.CreatedAt).ToList() });
            }
            return PartialView("_SpecificationsTable", new SpecificationIndexModel { Specifications = new List<Specification>() });
        }

        [HttpGet]
        [Route("editSpecification")]
        public async Task<IActionResult> EditSpecification(long? id, long? productToAdd)
        {
            var user = await GetUser();
            var datetime = DateTime.Now;

            var model =
                id == default
                ?
                (JsonConvert.DeserializeObject<SpecificationModel>(TempData["NewSpecification"] as string ?? string.Empty)
                ??
                new SpecificationModel
                {
                    Products = new List<SpecificationProductModel>(),
                    CreatedAt = datetime,
                    LastModified = datetime,
                    StartsAt = datetime,
                    ExpiresAt = datetime.AddDays(30),
                })
                :
                await _specificationService.GetSpecificationModel(user, id.Value);

            if ((model.VerifyState == VerifyState.Sended || model.VerifyState == VerifyState.Verified) && id != default)
            {
                return RedirectToAction("Index", "Specification", new { id = id });
            }

            if (productToAdd != default)
            {
                model.Products.Add(new SpecificationProductModel { Id = productToAdd.Value });
            }

            if (!model.Products.Any())
            {
                model.Products.Add(new SpecificationProductModel() { Id = productToAdd ?? default, Price = default });
            }

            var products = user.Products ?? new List<Product>();
            AddUnitsToViewBag(products);
            return View(model);

        }

        [HttpPost]
        [Route("editSpecification")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecification(SpecificationModel model, string SaveOption)
        {
            var user = await GetUser();
            if ((model.ExpiresAt - model.StartsAt)?.TotalDays < 7)
            {
                ModelState.AddModelError(nameof(model.ExpiresAt), "Слишком маленький период действия");
            }
            if (ModelState.IsValid)
            {
                if (model.VerifyState == VerifyState.Sended || model.VerifyState == VerifyState.Verified)
                {
                    return RedirectToAction("Index", "Specification", new { id = model.Id });
                }
                var mode = SpecificationSaveMode.Draft;
                if (SaveOption == "Отправить")
                {
                    mode = SpecificationSaveMode.New;
                }
                var product = await _specificationService.AddOrUpdateSpecification(user, model, mode);
                if (product != null)
                {
                    return RedirectToAction("Index", "Specification");
                }
                ModelState.AddModelError("", "Произошла ошибка добавления товара, попробуйте позже");
            }
            var products = user.Products ?? new List<Product>();
            if (!model.Products.Any()) model.Products.Add(new SpecificationProductModel() { Id = default, Price = default });
            AddUnitsToViewBag(products);
            return View(model);
        }

        [HttpPost]
        [Route("copySpecification")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopySpecification([FromForm] long specificationId)
        {
            var user = await GetUser();
            var model = await _specificationService.GetSpecificationModel(user, specificationId);
            model.Id = default;
            TempData["NewSpecification"] = JsonConvert.SerializeObject(model);
            return RedirectToAction("EditSpecification", "Specification");
        }

        [NonAction]
        private Task<User> GetUser()
        {
            var userName = User.Identity?.Name;
            return _userService.GetByEmail(userName);
        }

        [NonAction]
        private void AddUnitsToViewBag(List<Product>? products)
        {
            ViewBag.Products = products;
            ViewBag.ProductNames = products?.ToDictionary(c => c.Id, c => c.Name);
            //ViewBag.ProductNames = JsonConvert.SerializeObject(products?.ToDictionary(c => c.Id, c => c.Name), new JsonSerializerSettings
            //{
            //    PreserveReferencesHandling = PreserveReferencesHandling.Objects
            //});
        }
    }
}
