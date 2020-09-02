using System.Threading.Tasks;
using Stripe;

namespace Dogger.Tests.TestHelpers.Builders.Stripe
{
    public class TestCustomerBuilder
    {
        private readonly CustomerService customerService;

        public TestCustomerBuilder(
            CustomerService customerService)
        {
            this.customerService = customerService;
        }

        public async Task<Customer> BuildAsync()
        {
            return await this.customerService.CreateAsync(new CustomerCreateOptions());
        }
    }

}
