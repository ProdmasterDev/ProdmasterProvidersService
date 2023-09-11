using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels;
using ProvidersDomain.ViewModels.Home;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

namespace ProdmasterProvidersService.Controllers
{
    [Authorize]
    [Route("/")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeService _homeService;
        private readonly IUserService _userService;

        public HomeController(ILogger<HomeController> logger, IHomeService homeService, IUserService userService)
        {
            _logger = logger;
            _homeService = homeService;
            _userService = userService;
        }

        [HttpGet]
        [Route("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
        
        [HttpGet]
        [Route("standardDetail")]
        public async Task<IActionResult> StandardDetail(long id)
        {
            if (id != default)
            {
                var standard = await _homeService.GetStandart(id);
                return PartialView("_StandardDetailPartial", standard);
            }
            return BadRequest("id was null");
        }
        
        [HttpGet]
        [Route("standardAccordion")]
        public async Task<IActionResult> StandardAccordion([FromQuery]string? searchString)
        {
            StandartListModel model;
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                model = await _homeService.GetStandartListModel(searchString);                
            }
            else
            {
                model = await _homeService.GetStandartListModel();
            }            
            return PartialView("_StandardAccordionPartial", model);                
        }
        
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var model = await _homeService.GetStandartListModel();
            return View(model);
        }

        [HttpGet]
        [Route("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}