using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Infrastructure.Time;

namespace Dogger.Domain.Services.Amazon.Lightsail
{
    public class LightsailOperationService : ILightsailOperationService
    {
        private readonly IAmazonLightsail amazonLightsail;
        private readonly ITime time;

        public LightsailOperationService(
            IAmazonLightsail amazonLightsail,
            ITime time)
        {
            this.amazonLightsail = amazonLightsail;
            this.time = time;
        }

        public async Task WaitForOperationsAsync(
            IEnumerable<Operation> operations)
        {
            await WaitForOperationsAsync(
                operations.ToArray());
        }

        public async Task WaitForOperationsAsync(
            params Operation[] operations)
        {
            while(true)
            {
                operations = await RefreshStillRunningOperationsAsync(operations);
                if (operations.Length == 0)
                    return;

                var failedOperations = operations
                    .Where(x => x.Status == OperationStatus.Failed)
                    .ToArray();
                if (failedOperations.Length > 0)
                {
                    var operationExceptions = failedOperations.Select(operation =>
                        new LightsailOperationException(operation));

                    throw new LightsailOperationsException(
                        "One or more Lightsail operations failed.",
                        new AggregateException(operationExceptions));
                }

                await this.time.WaitAsync(1000);
            };
        }

        private async Task<Operation[]> RefreshStillRunningOperationsAsync(
            IEnumerable<Operation> operations)
        {
            var newOperations = await GetOperationsFromIdsAsync(operations
                .Where(x => !IsOperationStatusSuccessful(x.Status))
                .Select(x => x.Id));
            return newOperations.ToArray();
        }

        public async Task<IReadOnlyCollection<Operation>> GetOperationsFromIdsAsync(IEnumerable<string> ids)
        {
            var newOperationResponses = await Task.WhenAll(ids
                .Select(id => this.amazonLightsail
                    .GetOperationAsync(new GetOperationRequest()
                    {
                        OperationId = id
                    })));

            return newOperationResponses
                .Select(x => x.Operation)
                .ToArray();
        }

        private static bool IsOperationStatusSuccessful(OperationStatus status)
        {
            if (status == OperationStatus.Failed)
                return false;

            return
                status == OperationStatus.Completed ||
                status == OperationStatus.Succeeded;
        }
    }
}
