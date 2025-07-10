using Microsoft.AspNetCore.Mvc;

namespace ParkShare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController: ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("[action]")]
        public IActionResult Ping()
        {
            return Ok($"Pong -- {DateTime.UtcNow}");
        }        
    }
}
