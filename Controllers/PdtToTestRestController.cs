using KT_Learn.Controllers.Dtos;
using KT_Learn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KT_Learn.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // нужен валидный JWT: из него сервис берёт автора загрузки
    public class PdtToTestRestController : ControllerBase
    {
        private readonly IAITestMakerService aITestMakerService;

        public PdtToTestRestController(IAITestMakerService aITestMakerService)
        {
            this.aITestMakerService = aITestMakerService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTest([FromForm]AITestMakerRequest request, CancellationToken ct)
        {
            var answer = await aITestMakerService.AITestMakeFromPDF(request, ct);
            return Ok(
                new
                {
                    answer
                });
        }

        [HttpGet("getAiDraftTests")]
        public async Task<ActionResult> GetDraftTest()
        {
            return Ok(
                await aITestMakerService.GetAIDraftTest());
        }

    }
}
