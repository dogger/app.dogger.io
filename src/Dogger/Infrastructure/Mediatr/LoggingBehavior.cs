﻿using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Serilog;

namespace Dogger.Infrastructure.Mediatr
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger logger;

        public LoggingBehavior(
            ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            logger.Verbose("Executing request {CommandName}", request.GetType().Name);

            var result = await next();

            logger.Verbose("Executed request {CommandName}", request.GetType().Name);

            return result;
        }
    }
}
