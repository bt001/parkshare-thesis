using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Users;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [ApiController]
    public class UserController : BaseApiController
    {
        private readonly ILogger<TestController> _logger;

        public UserController(ILogger<TestController> logger, IMediator mediator, IMapper mapper) : base(mediator, mapper)
        {
            _logger = logger;
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(GetUserByEmailResult), 200)]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
            => await ProcessQueryAsync<GetUserByEmailRequest, GetUserByEmailResult, GetUserByEmailQuery, GetUserByEmailResponse>(new GetUserByEmailRequest { Email = email });
    }
}
