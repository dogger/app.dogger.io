using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Events.InstanceDeleted
{
    public class InstanceDeletedEvent : IRequest
    {
        public Instance DatabaseInstance { get; }

        public InstanceDeletedEvent(
            Instance databaseInstance)
        {
            this.DatabaseInstance = databaseInstance;
        }
    }
}
