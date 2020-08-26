using Dogger.Domain.Models.Builders;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class UserBuilder : ModelBuilder<User>
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

        public override User Build()
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
