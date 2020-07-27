using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class OctokitUserBuilder
    {
        private string? login;

        public OctokitUserBuilder WithLogin(string login)
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
                default,
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
