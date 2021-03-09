using Atlassian.Jira;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgileBot.Models;

namespace AgileBot
{
    public static class JiraStatus
    {
        public const string ToDo = "To Do";
        public const string OpenBug = "Open Bug";
        public const string InProgress = "In Progress";
        public const string InReview = "In Review";
        public const string Done = "Done";
    }

    public static class JiraIssueType
    {
        public const string Task = "Task";
        public const string Bug = "Bug";
        public const string Epic = "Epic";
    }

    public class AtlassianClient
    {
        public static Color JiraBlue => new Color(0, 82, 204);

        public AtlassianSettings Settings { get; private set; }

        public Jira JiraClient { get; private set; }

        public AtlassianClient(AtlassianSettings settings)
        {
            Settings = settings;
            JiraRestClientSettings set = new JiraRestClientSettings();
            set.EnableRequestTrace = true;
            JiraClient = Jira.CreateOAuthRestClient(Settings.JiraURL, Settings.ConsumerKey, Settings.ConsumerSecret, Settings.OAuthAccessToken, Settings.OAuthAccessSecret, settings: set);   
        }

        public async Task<bool> TryExecuteJiraCommand(string[] command, SocketUserMessage e)
        {
            var key = command.FirstOrDefault();
            if(key.Contains("-"))
            {
                return await PostJiraTask(key, e);
            }
            else
            {
                return await GetJiraProject(key, e);
            }
        }

        public async Task<bool> GetJiraProject(string key, SocketUserMessage e)
        {
            key = key.ToUpper();
            Project proj;
            try
            {
                proj = await JiraClient.Projects.GetProjectAsync(key);
            }
            catch
            {
                return false;
            }
            await e.Channel.TriggerTypingAsync();
            //Need custom REST request to get project description.
            var projectInfo = await GetProjectInfo(key);
            var boards = await GetAllBoards();
            var board = boards?.values.FirstOrDefault(b => b.name.StartsWith(proj.Name));
            string url;
            if(board != null)
            {
                url = $"{JiraClient.Url}secure/RapidBoard.jspa?rapidView={board.id}&projectKey={key}";
            }
            else
            {
                url = $"{JiraClient.Url}projects/{key}";
            }

            var file = JiraClient.RestClient.DownloadData(proj.AvatarUrls.Large);
            
            
            using(MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(file, 0, file.Length);
                memStream.Seek(0, SeekOrigin.Begin);

                var avatarFileName = "avatar.png";

                var builder = new EmbedBuilder();
                builder.WithAuthor(proj.Key, url:url);
                builder.WithThumbnailUrl($"attachment://{avatarFileName}");
                builder.WithTitle(proj.Name);
                builder.WithUrl(url);
                builder.WithDescription(projectInfo?.description);
                builder.WithColor(JiraBlue);
                
                await e.Channel.SendFileAsync(memStream, avatarFileName, embed: builder.Build());
            }
            return true;
        }

        public async Task<bool> PostJiraTask(string key, SocketUserMessage e)
        {
            var issue = await GetJiraTask(key);
            if(issue == null) return false;
            await e.Channel.TriggerTypingAsync();            
            await e.Channel.SendMessageAsync(embed: GetEmbedForJiraIssue(issue));
            return true;
        }

        public async Task<Issue?> GetJiraTask(string key)
        {
            Issue? issue = null;
            try
            {
                issue = await JiraClient.Issues.GetIssueAsync(key);
            }
            catch
            {
            }
            return issue;
        }

        public string GetURLForIssue(ComparableString key)
        {
            return $"{AgileBot.Atlassian.JiraClient.Url}browse/{key}";
        }

        public async Task<List<Issue>> GetAllJiraIssuesForUser(string jql, int maxIssues = 10)
        {
            var issues = await JiraClient.Issues.GetIssuesFromJqlAsync(jql, maxIssues);
            return issues.ToList();
        }

        public EmbedBuilder GetEmbedBuilderForJiraIssue(Issue issue)
        {
            var builder = new EmbedBuilder();
            var user = issue.AssigneeUser;
            var assignee = string.Empty;
            string? assigneeAvatarURL = null;
            if(user != null)
            {
                assignee = user.DisplayName;
                assigneeAvatarURL = user.AvatarUrls.Large;
            }
            string? reporterAvatarURL = null;
            user = issue.ReporterUser;
            var reporter = string.Empty;
            if(user != null)
            {
                reporter = user.DisplayName;
                reporterAvatarURL = user.AvatarUrls.Large;
            }
            ///TODO: Fetch image urls/attachments and embed them
            //var attachments = await issue.GetAttachmentsAsync();
            //foreach(var attach in attachments)
            //{
            //    //if(attach.MimeType == )
            //}
            var url = $"{JiraClient.Url}browse/{issue.Key}";
            builder.WithAuthor(assignee, assigneeAvatarURL, url);
            builder.WithTitle($"{issue.Key} - {issue.Status}");
            builder.AddField(issue.Summary, string.IsNullOrEmpty(issue.Description?.Trim()) ? "\u200b" : issue.Description);
            builder.WithFooter(reporter, reporterAvatarURL);
            builder.WithUrl(url);
            if(issue.Created != null) builder.WithTimestamp(new DateTimeOffset(issue.Created.Value));
            builder.Color = JiraBlue;
            return builder;
        }

        public Embed GetEmbedForJiraIssue(Issue issue)
        {
            return GetEmbedBuilderForJiraIssue(issue).Build();
        }

        public async Task<ProjectInfo?> GetProjectInfo(string projectKey)
        {
            var url = $"{JiraClient.Url}rest/api/2/project/{projectKey}";
            var restRequest = new RestRequest(url, Method.GET, DataFormat.Json);
            try
            {
                var resp = await JiraClient.RestClient.ExecuteRequestAsync(restRequest);
                var respData = JsonConvert.DeserializeObject<ProjectInfo>(resp.Content);
                return respData;
            }
            catch(Exception except)
            {
                Console.WriteLine($"Error: {except.Message}");
            }
            return null;
        }

        public async Task<IssueStatus?> GetStatus(string status)
        {
            try
            {
                return await JiraClient.Statuses.GetStatusAsync(status);
            }
            catch
            {
                return null;
            }
        }

        public async Task<ListBoardsResponseInfo?> GetAllBoards()
        {
            ListBoardsResponseInfo? respData = null;
            var url = $"{JiraClient.Url}rest/agile/latest/board/";
            var restRequest = new RestRequest(url, Method.GET, DataFormat.Json);
            try
            {
                var resp = await JiraClient.RestClient.ExecuteRequestAsync(restRequest);
                respData = JsonConvert.DeserializeObject<ListBoardsResponseInfo>(resp.Content);
                return respData;
            }
            catch(Exception except)
            {
                Console.WriteLine($"Error: {except.Message}");
            }
            return respData;
        }
    }
}
