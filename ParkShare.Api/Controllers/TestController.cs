using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace ParkShare.Api.Controllers
{
    [ApiController]
    public class TestController: BaseApiController
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger, IMediator mediator, IMapper mapper) : base(mediator, mapper)
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
