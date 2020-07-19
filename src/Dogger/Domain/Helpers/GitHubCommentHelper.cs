using System.Collections.Generic;
using System.Linq;

namespace Dogger.Domain.Helpers
{
    public static class GitHubCommentHelper
    {
        public static string RenderSpoiler(string title, string content)
        {
            return $"<details>\n<summary><b>{title}</b></summary>\n\n{content}\n</details>";
        }

        public static string RenderList(IEnumerable<string> filePathsInCodeElement)
        {
            return string.Join("\n", filePathsInCodeElement
                .Select(x => $"- {x}"));
        }
    }
}
