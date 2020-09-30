namespace Dogger.Domain.Controllers.PullDog.Webhooks.Models
{
    public class InstallationPayload
    {
        public long Id { get; set; }

        public UserPayload? Account { get; set; } = null!;
    }
}
