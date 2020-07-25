namespace Dogger.Infrastructure.Mediatr.Tracing
{
    public interface ITraceableRequest
    {
        string? TraceId { get; set; }
    }
}
