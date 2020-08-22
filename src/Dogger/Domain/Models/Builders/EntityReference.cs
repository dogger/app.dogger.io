using System;

namespace Dogger.Domain.Models.Builders
{
    public class EntityReference<TModel> where TModel : class
    {
        public EntityReference(TModel? reference)
        {
            this.Reference = reference;
        }

        public EntityReference(Guid? id)
        {
            this.Id = id;
        }

        public Guid? Id { get; }
        public TModel? Reference { get; }
    }
}
