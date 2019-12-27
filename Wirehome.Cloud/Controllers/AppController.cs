using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.App;

namespace Wirehome.Cloud.Controllers
{
    [ApiController]
    public class AppController : Controller
    {
        [HttpGet]
        [Route("api/v1/app/access_type")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult<string> GetAccessType()
        {
            // This API will always return "remote" because it IS remote access if this point is reached.
            // Wirehome.Core will override this API and return "local" always. Using this same URI the
            // app can properly distinguish.
            return "remote";
        }
    }
}
