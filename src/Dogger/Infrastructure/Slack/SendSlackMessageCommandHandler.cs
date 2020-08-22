using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Infrastructure.Slack
{
    public class SendSlackMessageCommandHandler : IRequestHandler<SendSlackMessageCommand, Unit>
    {
        private readonly ISlackClient? slackClient;

        public SendSlackMessageCommandHandler(
            IOptionalService<ISlackClient> slackClient)
        {
            this.slackClient = slackClient.Value;
        }

        public async Task<Unit> Handle(
            SendSlackMessageCommand request,
            CancellationToken cancellationToken)
        {
            if (this.slackClient == null)
                return Unit.Value;

            await this.slackClient.PostAsync(new SlackMessage()
            {
                Attachments = new List<SlackAttachment>()
                {
                    new SlackAttachment()
                    {
                        Fields = request.Fields?.ToList()
                    }
                },
                Text = request.Message
            });

            return Unit.Value;
        }
    }
}

