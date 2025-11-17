using Dtos.Countries;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly ICountriesService countriesService;

        public CountriesController(ICountriesService countriesService)
        {
            this.countriesService = countriesService;
        }

        /// <summary>
        /// Retourne la liste des pays, avec le nom localisé.
        /// </summary>
        /// <param name="lang">
        /// Code langue (fr, en, de, es, it, nl, pl, pt).
        /// Si null => langue par défaut serveur (en).
        /// </param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CountryDto>>> GetCountries([FromQuery] string? lang = null)
        {
            IEnumerable<CountryDto> countries = await countriesService.GetAllAsync(lang);
            return Ok(countries);
        }
    }
}