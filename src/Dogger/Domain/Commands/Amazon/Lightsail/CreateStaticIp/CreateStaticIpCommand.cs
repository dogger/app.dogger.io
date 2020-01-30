using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.CreateStaticIp
{
    public class CreateStaticIpCommand : IRequest
    {
        public string Name { get; set; }

        public CreateStaticIpCommand(
            string name)
        {
            this.Name = name;
        }
    }
}
