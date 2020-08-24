using System;

namespace Dogger.Domain.Models.Builders
{
    public class IdentityBuilder : ModelBuilder<Identity>
    {
        private Guid id;

        private string? name;

        private EntityReference<User>? user;

        public IdentityBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public IdentityBuilder WithName(string value)
        {
            this.name = value;
            return this;
        }

        public IdentityBuilder WithUser(User value)
        {
            this.user = value;
            return this;
        }

        public IdentityBuilder WithUser(Guid value)
        {
            this.user = new EntityReference<User>(value);
            return this;
        }

        public override Identity Build()
        {
            return new Identity()
            {
                Id = id,
                Name = name ?? throw new InvalidOperationException("Name is not set."),
                User = user?.Reference!,
                UserId = user?.Id ?? default
            };
        }
    }
}
