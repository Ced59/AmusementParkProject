using Microsoft.AspNetCore.Mvc;

namespace GetApisDatas.WebAPI.Controllers
{
    public class CaptainCoasterController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
