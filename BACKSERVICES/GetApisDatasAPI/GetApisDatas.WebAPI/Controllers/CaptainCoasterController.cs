using GetApisDatas.WebAPI.Settings;
using Microsoft.AspNetCore.Mvc;

namespace GetApisDatas.WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(1)]
    [Route("[controller]")]
    public class CaptainCoasterController : ControllerBase
    {
        [HttpGet]
        [Route("get-all-captain-coaster-datas")]
        public IActionResult GetCaptainCoasterFormattedDatas()
        {
            return Ok();
        }
    }
}