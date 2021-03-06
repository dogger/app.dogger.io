﻿using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Dogger.Infrastructure.Mediatr.Tracing
{

    public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public TracingBehavior(
            IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request is ITraceableRequest traceableRequest && traceableRequest.TraceId == null)
            {
                traceableRequest.TraceId = this.httpContextAccessor.HttpContext?.TraceIdentifier;

                using (LogContext.PushProperty("RequestTraceId", traceableRequest.TraceId))
                {
                    return await next();
                }
            }

            return await next();
        }
    }
}
