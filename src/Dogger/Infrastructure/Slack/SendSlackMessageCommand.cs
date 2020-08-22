using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Infrastructure.Slack
{
    public class SendSlackMessageCommand : IRequest<Unit>
    {
        public string Message { get; }

        public IEnumerable<SlackField>? Fields { get; set; }

        public SendSlackMessageCommand(
            string message)
        {
            this.Message = message;
        }
    }
}
