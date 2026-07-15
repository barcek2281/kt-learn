using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KT_Learn.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthRestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Hello()
        {
            return Ok(new { message = "Hello" });
        }
    }
}
