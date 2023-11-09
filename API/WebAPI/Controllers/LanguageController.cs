using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageController : ControllerBase
    {
        private readonly IStringLocalizer<LanguageController> _localizer;
        private readonly IStringLocalizer _errorsLocalizer;
        private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

        public LanguageController(IStringLocalizer<LanguageController> localizer, IStringLocalizerFactory factory, IOptions<RequestLocalizationOptions> localizationOptions)
        {
            _localizer = localizer;
            _errorsLocalizer = factory.Create("ErrorMessages", Assembly.GetExecutingAssembly().GetName().Name!);
            _localizationOptions = localizationOptions;
        }


        [HttpGet("get-languages")]
        public IActionResult GetLanguages()
        {
            var cultures = _localizationOptions.Value.SupportedCultures!.Select(c => c.Name).ToList();

            return Ok(cultures);
        }

        [HttpGet("get-language")]
        public IActionResult GetLanguage()
        {
            var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var culture = requestCultureFeature!.RequestCulture.Culture.Name;

            return Ok(culture);
        }



        [HttpPost("set-language")]
        public IActionResult SetLanguage([FromBody]string? culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return BadRequest(_errorsLocalizer["NullCultureErrorMessage"]);
            }

            try
            {
                var cultureInfo = new CultureInfo(culture);
                var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureInfo));

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    cookieValue,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                //TODO: Coté front, il faudra rafraichir la page pour que la culture soit prise en compte

                return Ok(_localizer["CultureSetMessage", culture]);
            }
            catch (CultureNotFoundException)
            {
                return BadRequest(_errorsLocalizer["InvalidCultureErrorMessage"]);
            }
        }
    }
}
