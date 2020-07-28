using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class UserBuilder
    {
        private string? login;
        private int id;

        public UserBuilder WithId(int id)
        {
            this.id = id;
            return this;
        }

        public UserBuilder WithLogin(string login)
        {
            this.login = login;
            return this;
        }

        public User Build()
        {
            return new User(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                id,
                default,
                login,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default);
        }
    }
}
