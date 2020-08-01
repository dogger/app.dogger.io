using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class RepositoryBuilder
    {
        private User? user;
        private string? name;
        private long id;
        private string? fullName;

        public RepositoryBuilder WithUser(User user)
        {
            this.user = user;
            return this;
        }

        public RepositoryBuilder WithId(long id)
        {
            this.id = id;
            return this;
        }

        public RepositoryBuilder WithName(string name)
        {
            this.name = name;
            return this;
        }

        public RepositoryBuilder WithFullName(string fullName)
        {
            this.fullName = fullName;
            return this;
        }

        public Repository Build()
        {
            return new Repository(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                id,
                default,
                user,
                name,
                fullName,
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
                default);
        }
    }
}
