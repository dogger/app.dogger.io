namespace Dogger.Infrastructure.Ioc
{
    public interface IOptionalService<out T> where T : class
    {
        bool IsConfigured { get; }

        T? Value { get; }
    }
}
