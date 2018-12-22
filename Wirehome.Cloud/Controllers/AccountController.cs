using Microsoft.AspNetCore.Mvc;

namespace Wirehome.Cloud.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ViewResult Index()
        {
            return View("Index");
        }

        [HttpPost]
        public ViewResult Login()
        {
            foreach (var key in HttpContext.Request.Form.Keys)
            {

            }

            return View("Index");
        }
    }
}
