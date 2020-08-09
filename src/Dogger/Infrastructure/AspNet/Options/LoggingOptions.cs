using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class LoggingOptions
    {
        [NotLogged]
        public string? ElasticsearchLoggingUrl { get; set; }
    }
}
