using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.CreateDomain
{
    public class CreateDomainCommand : IRequest
    {
        public string HostName { get; set; }

        public CreateDomainCommand(
            string hostName)
        {
            this.HostName = hostName;
        }
    }
}
