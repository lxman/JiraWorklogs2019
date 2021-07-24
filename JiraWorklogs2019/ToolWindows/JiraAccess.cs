using Atlassian.Jira;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiraWorklogs2019.ToolWindows
{
    internal class JiraAccess
    {
        private Jira JiraInstance;
        private const string Query = "resolution = Unresolved AND assignee = ";
        private List<Issue> Results = new();
        private readonly string UserName;
        private readonly string Password;

        internal JiraAccess(string uri, string userName, string password)
        {
            JiraInstance = Jira.CreateRestClient(uri, userName, password);
            JiraInstance.Issues.MaxIssuesPerRequest = 10;
            UserName = userName;
            Password = password;
        }

        internal async Task GetDataAsync()
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password)) return;
            Results.Clear();
            await GetIssuesAsync(JiraInstance);
            Results = Results.Where(r =>
                (r.TimeTrackingData.OriginalEstimateInSeconds ?? -1) > (r.TimeTrackingData.TimeSpentInSeconds ?? 0)).ToList();
        }

        internal IEnumerable<string> GetStatuses()
        {
            return Results.Select(r => r.Status.Name).Distinct();
        }

        internal IEnumerable<Issue> GetIssuesForStatus(string status)
        {
            return Results.Where(r => r.Status.Name == status);
        }

        private async Task GetIssuesAsync(Jira jira)
        {
            int itemsLoaded = 0;
            IPagedQueryResult<Issue> issues = await PageIssuesAsync(jira, itemsLoaded);
            while (itemsLoaded < issues.TotalItems)
            {
                if (itemsLoaded > 0) issues = await PageIssuesAsync(jira, itemsLoaded);
                Results.AddRange(issues);
                itemsLoaded += issues.Count();
            }
        }

        private async Task<IPagedQueryResult<Issue>> PageIssuesAsync(Jira jira, int startAt)
        {
            if (string.IsNullOrEmpty(UserName)) return null;
            return await JiraInstance.Issues.GetIssuesFromJqlAsync($"{Query}{UserName}", startAt: startAt);
        }
    }
}