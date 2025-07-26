using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Auth
{
    public class LoginRequest : IApiRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
