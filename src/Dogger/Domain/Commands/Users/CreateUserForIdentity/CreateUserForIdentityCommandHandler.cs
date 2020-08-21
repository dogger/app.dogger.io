using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Commands.Users.CreateUserForIdentity
{
    public class CreateUserForIdentityCommandHandler : IRequestHandler<CreateUserForIdentityCommand, User>
    {
        private readonly DataContext dataContext;
        private readonly CustomerService? stripeCustomerService;

        public CreateUserForIdentityCommandHandler(
            DataContext dataContext,
            IOptionalService<CustomerService> stripeCustomerService)
        {
            this.dataContext = dataContext;
            this.stripeCustomerService = stripeCustomerService.Value;
        }

        public async Task<User> Handle(CreateUserForIdentityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email not specified.");

            if (string.IsNullOrWhiteSpace(request.IdentityName))
                throw new InvalidOperationException("IdentityName not specified.");

            var user = await AddUserToDatabaseAsync(request);
            await AssignStripeCustomerToUserAsync(user, request);

            return user;
        }

        private async Task AssignStripeCustomerToUserAsync(User user, CreateUserForIdentityCommand request)
        {
            if (this.stripeCustomerService == null)
                return;

            try
            {
                var existingCustomer = await GetExistingStripeCustomerAsync(request);
                if (existingCustomer != null)
                {
                    user.StripeCustomerId = existingCustomer.Id;
                }
                else
                {
                    await CreateNewStripeCustomerForUserAsync(user, request);
                }
            }
            finally
            {
                await this.dataContext.SaveChangesAsync();
            }
        }

        private async Task<Customer?> GetExistingStripeCustomerAsync(CreateUserForIdentityCommand request)
        {
            if (EnvironmentHelper.IsRunningInTest || Debugger.IsAttached)
                return null;

            if (this.stripeCustomerService == null)
                return null;

            return await this.stripeCustomerService
                .ListAutoPagingAsync(new CustomerListOptions()
                {
                    Email = request.Email.ToLowerInvariant()
                })
                .FirstOrDefaultAsync();
        }

        private async Task CreateNewStripeCustomerForUserAsync(User user, CreateUserForIdentityCommand request)
        {
            if (this.stripeCustomerService == null)
                return;

            var customer = await this.stripeCustomerService.CreateAsync(new CustomerCreateOptions()
            {
                Email = request.Email.ToLowerInvariant(),
                Metadata = new Dictionary<string, string?>()
                {
                    { "UserId", user.Id.ToString() },
                    { "CreatedByIdentityName", request.IdentityName }
                }
            });
            user.StripeCustomerId = customer.Id;
        }

        private async Task<User> AddUserToDatabaseAsync(CreateUserForIdentityCommand request)
        {
            var user = new User() {
                StripeCustomerId = string.Empty
            };

            var identity = new Identity()
            {
                Name = request.IdentityName,
                User = user
            };

            user.Identities.Add(identity);

            await this.dataContext.Identities.AddAsync(identity);
            await this.dataContext.Users.AddAsync(user);

            await this.dataContext.SaveChangesAsync();

            return user;
        }
    }
}
