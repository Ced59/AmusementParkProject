using Microsoft.AspNetCore.Mvc;

namespace GetDatasAPI.WebAPI.Controllers
{
    public class CaptainCoasterController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
