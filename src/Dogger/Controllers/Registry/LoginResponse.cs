using Destructurama.Attributed;

namespace Dogger.Controllers.Registry
{
    public class LoginResponse
    {
        public string? Username { get; set; }

        [NotLogged]
        public string? Password { get; set; }

        public string? Url { get; set; }
    }
}
