using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dogger.Domain.Helpers
{
    public static class GitHubCommentHelper
    {
        public static string RenderSpoiler(string title, string content)
        {
            return $"<details>\n<summary><b>{title}</b></summary>\n\n{content}\n</details>";
        }
    }
}
