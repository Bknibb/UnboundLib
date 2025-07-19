using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Utils
{
    static class GithubUpdateChecker
    {
        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("UnboundLib"));
        public async static Task<bool> CheckForUpdates(string repoOwner, string repoName, string currentVersion)
        {
            var release = await client.Repository.Release.GetLatest(repoOwner, repoName);
            var latestVersion = new Version(release.TagName.Trim('v'));
            var currentVersionObj = new Version(currentVersion);
            if (latestVersion > currentVersionObj)
            {
                return true;
            }
            return false;
        }
    }
}
