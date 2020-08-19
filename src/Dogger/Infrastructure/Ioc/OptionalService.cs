namespace Dogger.Infrastructure.Ioc
{
    public class OptionalService<T> : IOptionalService<T> where T : class
    {
        private readonly T? value;

        public OptionalService(T? value)
        {
            this.value = value;
        }

        public bool IsConfigured => this.value != null;

        public T? Value => value;
    }
}
