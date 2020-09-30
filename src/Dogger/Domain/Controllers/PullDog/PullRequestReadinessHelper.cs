using Octokit;

namespace Dogger.Domain.Controllers.PullDog
{
    public static class PullRequestReadinessHelper
    {
        public static bool IsReady(
            bool isDraft,
            string state,
            string? accountType)
        {
            if (isDraft)
                return false;

            if(state != "open")
                return false;

            if (accountType == AccountType.Bot.ToString())
                return false;

            return true;
        }
    }
}
