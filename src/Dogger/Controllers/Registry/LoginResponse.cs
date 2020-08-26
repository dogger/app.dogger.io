using Destructurama.Attributed;

namespace Dogger.Controllers.Registry
{
    public class LoginResponse
    {
        public LoginResponse(
            string username, 
            string password, 
            string url)
        {
            this.Username = username;
            this.Password = password;
            this.Url = url;
        }

        public string Username { get; set; }

        [NotLogged]
        public string Password { get; set; }

        public string Url { get; set; }
    }
}
