using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Infrastructure;

namespace Dogger.Domain.Services.Provisioning
{
    public class JobQueue
    {
        private readonly ConcurrentDictionary<string, FirstLastQueue<IProvisioningJob>> groupedQueues;

        public JobQueue()
        {
            groupedQueues = new ConcurrentDictionary<string, FirstLastQueue<IProvisioningJob>>();;
        }

        public void Enqueue(
            IProvisioningJob job, 
            string groupingKey)
        {
            var queue = this.groupedQueues.GetOrAdd(
                groupingKey, 
                new FirstLastQueue<IProvisioningJob>());
            queue.Enqueue(job);
        }

        public IProvisioningJob? Peek()
        {
            var queue = this.groupedQueues.Values.FirstOrDefault();
            if (queue == null)
                return null;

            var job = queue.Peek();
            while (GetShouldJobBeCleanedUp(job))
            {
                queue.Dequeue();
                job = queue.Peek();
            }

            if (job == null)
                return null;
        }

        private static void CleanupQueue(FirstLastQueue<IProvisioningJob> queue)
        {
            throw new InvalidOperationException();
        }

        private static bool GetShouldJobBeCleanedUp(IProvisioningJob? job)
        {
            return
                job == null ||
                job.IsEnded;
        }
    }
}
