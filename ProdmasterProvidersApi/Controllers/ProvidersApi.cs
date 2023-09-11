using Microsoft.AspNetCore.Mvc;
using ProvidersDomain.Models;
using ProvidersDomain.Services;

namespace ProdmasterProvidersApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProvidersApi : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<ProvidersApi> _logger;
        private readonly ISpecificationService _specificationService;

        public ProvidersApi(ILogger<ProvidersApi> logger, ISpecificationService specificationService)
        {
            _logger = logger;
            _specificationService = specificationService;
        }

        [HttpGet(Name = "specifications")]
        public Task<IEnumerable<Specification>> GetSpecifications()
        {
            return _specificationService.GetNewSpecifications();
        }

        [HttpPut(Name = "specifications")]
        public IEnumerable<WeatherForecast> SetSpecifications()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}