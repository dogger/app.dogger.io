﻿using System.Threading.Tasks;
using Stripe;

namespace Dogger.Tests.TestHelpers.Builders.Stripe
{

    public class TestPaymentMethodBuilder
    {
        private readonly PaymentMethodService paymentMethodService;

        private Customer customer;

        public TestPaymentMethodBuilder(
            PaymentMethodService paymentMethodService)
        {
            this.paymentMethodService = paymentMethodService;
        }

        public TestPaymentMethodBuilder WithCustomer(Customer customer)
        {
            this.customer = customer;
            return this;
        }

        public async Task<PaymentMethod> BuildAsync()
        {
            var paymentMethod = await this.paymentMethodService.CreateAsync(new PaymentMethodCreateOptions()
            {
                Type = "card",
                Card = new PaymentMethodCardCreateOptions()
                {
                    Cvc = "123",
                    ExpMonth = 11,
                    ExpYear = 2030,
                    Number = "4242424242424242"
                }
            });

            await this.paymentMethodService.AttachAsync(
                paymentMethod.Id,
                new PaymentMethodAttachOptions()
                {
                    Customer = this.customer.Id
                });

            return paymentMethod;
        }
    }
}