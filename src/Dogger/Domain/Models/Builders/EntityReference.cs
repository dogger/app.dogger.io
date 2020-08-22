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

        public static implicit operator TModel(EntityReference<TModel> entityReference)
        {
            return entityReference.Reference!;
        }

        public static implicit operator Guid(EntityReference<TModel> entityReference)
        {
            return entityReference.Id!.Value;
        }

        public static implicit operator Guid?(EntityReference<TModel> entityReference)
        {
            return entityReference.Id;
        }

        public static implicit operator EntityReference<TModel>(TModel model)
        {
            return new EntityReference<TModel>(model);
        }
    }
}
