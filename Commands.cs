using Atlassian.Jira;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command(nameof(Roll))]
        public async Task Roll() => await Roll(100);
        [Command(nameof(Roll))]
        public async Task Roll(int max) => await Roll(1, max);
        [Command(nameof(Roll))]
        public async Task Roll(int min, int max)
        {
            var ctx = Context;
            await ctx.Channel.TriggerTypingAsync();
            var rand = new Random();
            var result = rand.Next(min, max + 1);
            await ctx.Channel.SendMessageAsync($"🎲 {ctx.Message.Author.Username} rolled {result}! [{min} to {max}]");
        }

        [Command("Bugs"), Priority(1)]
        public async Task OnBugs(string? project = null, TaskParams? parameters = null)
        {
            parameters = parameters ?? new TaskParams();
            parameters.Project = project;
            await OnBugs(parameters);
        }
        [Command("Bugs"), Priority(2)]
        public async Task OnBugs(TaskParams parameters)
        {
            if(parameters.User == null)
            {
                parameters.title = "Bugs!";
            }
            else
            {
                parameters.title = "Bugs for {0}";
            }

            if(parameters.Status == null) parameters.Status = string.Empty;
            parameters.PreJql = $"(status = \"{JiraStatus.OpenBug}\" OR status = \"{JiraStatus.InProgress}\")";
            parameters.Type = JiraIssueType.Bug;
            if(parameters.User == null)
            {
                parameters.User = string.Empty;
            }
            await OnTasks(parameters);
        }
        [Command("MyBugs")]
        public async Task OnMyBugs(string? project = null, TaskParams? parameters = null)
        {
            parameters = parameters ?? new TaskParams();
            parameters.title = "Bugs for {0}";
            parameters.Project = project;
            if(parameters.Status == null) parameters.Status = string.Empty;
            parameters.PreJql = $"(status = \"{JiraStatus.OpenBug}\" OR status = \"{JiraStatus.InProgress}\")";
            parameters.Type = JiraIssueType.Bug;
            await OnTasks(parameters);
        }
        [Command("Todo")]
        public async Task OnTodo(string? project = null, TaskParams? parameters = null)
        {
            parameters = parameters ?? new TaskParams();
            parameters.Project = project;
            parameters.Status = JiraStatus.ToDo;

            await OnTasks(parameters);
        }
        [Command("Tasks"), Priority(0)]
        public async Task OnTasks(string? project = null, string status = JiraStatus.InProgress, TaskParams? parameters = null)
        {
            parameters = parameters ?? new TaskParams();
            parameters.Status = status;

            await OnTasks(project, parameters);
        }
        [Command("Tasks"), Priority(1)]
        public async Task OnTasks(string? project = null, TaskParams? parameters = null)
        {
            parameters = parameters ?? new TaskParams();
            parameters.Project = project;

            await OnTasks(parameters);
        }
        [Command("Tasks"), Priority(2)]
        public async Task OnTasks(TaskParams parameters)
        {
            var ctx = Context;
            await ctx.Channel.TriggerTypingAsync();
            if(parameters.User == null)
            {
                parameters.User = ctx.User.Username;
            }
            if(parameters.Status == null)
            {
                parameters.Status = JiraStatus.InProgress;
            }
            var jql = parameters.ToJQL();
            List<Issue> tasks;
            try
            {
                tasks = await AgileBot.Atlassian.GetAllJiraIssuesForUser(jql, parameters.Max);
            }
            catch(Exception except)
            {
                await ctx.Channel.SendMessageAsync($"Exception: {except.Message}");
                return;
            }
            if(tasks.Count != 0)
            {
                var iter = tasks.GetEnumerator();
                iter.MoveNext();
                var key = iter.Current.Key.ToString();
                var titleUrl = new Uri($"{AgileBot.Atlassian.JiraClient.Url}browse/{key}?jql={jql}");
                var linkName = $"**{key}**: {iter.Current.Summary}";
                var user = iter.Current.AssigneeUser;
                var msg = $"{GetHyperlink(linkName, AgileBot.Atlassian.GetURLForIssue(iter.Current.Key))}";
                while(iter.MoveNext())
                {
                    key = iter.Current.Key.ToString();
                    linkName = $"**{key}**: { iter.Current.Summary}";
                    msg += $"\n{GetHyperlink(linkName, AgileBot.Atlassian.GetURLForIssue(iter.Current.Key))}";
                }
                var builder = new EmbedBuilder();
                string avatarUrl = string.Empty;
                string author = parameters.title;
                if(user != null)
                {
                    avatarUrl = user.AvatarUrls.Large;
                    author = string.Format(parameters.title, user.DisplayName);
                }

                builder.WithAuthor(author, avatarUrl, titleUrl.AbsoluteUri);
                builder.WithDescription(msg);
                builder.WithColor(AtlassianClient.JiraBlue);
                await ctx.Channel.SendMessageAsync(embed: builder.Build());
            }
            else
            {
                await ctx.Channel.SendMessageAsync("No tasks found.");
            }
        }

        //TODO: [Command("Update")]
        public async Task OnUpdate(string key, string status)
        {
            var ctx = Context;
            await ctx.Channel.TriggerTypingAsync();
            Issue? issue = await AgileBot.Atlassian.GetJiraTask(key);
            if(issue == null)
            {
                await ctx.Channel.SendMessageAsync($"Failed to find issue: {key}");
                return;
            }
            var jiraStatus = await AgileBot.Atlassian.GetStatus(status);
            if(issue == null)
            {
                await ctx.Channel.SendMessageAsync($"Failed to find status: {status}");
                return;
            }
            var embed = AgileBot.Atlassian.GetEmbedBuilderForJiraIssue(issue);
            
            await issue.SetPropertyAsync("Status", JToken.FromObject(jiraStatus));
            await issue.SaveChangesAsync();
            embed.Title += $" -> {jiraStatus?.Name}";
            await ctx.Channel.SendMessageAsync(embed: embed.Build());
        }

        string GetHyperlink(string name, string url)
        {
            return $"[{name}]({url})";
        }

        [NamedArgumentType]
        public class TaskParams
        {
            public string title = "Tasks for {0}";
            public int Max { get; set; } = 10;
            public string? User { get; set; }
            public string? Project { get; set; }
            public string? Type { get; set; }
            public string? Status { get; set; }
            public string OrderBy { get; set; } = "priority";
            public string Order { get; set; } = "desc";
            public string PreJql { get; set; } = default!;
            public string Jql { get; set; } = default!;

            public string ToJQL()
            {
                if(!string.IsNullOrEmpty(Jql))
                {
                    return Jql;
                }
                string newJQL = string.Empty;
                if(!string.IsNullOrEmpty(User))
                {
                    var lower = User.ToLower();
                    if(AgileBot.DiscordSettings.UserMap.TryGetValue(lower, out var mappedUser))
                    {
                        User = mappedUser;
                    }
                    newJQL = JoinAnd(PreJql);
                    newJQL += $"assignee = \"{User}\"";
                }
                if(!string.IsNullOrEmpty(Status))
                {
                    var lower = Status.ToLower();
                    if (AgileBot.DiscordSettings.StatusMap.TryGetValue(lower, out var mappedStatus))
                    {
                        Status = mappedStatus;
                    }
                    newJQL = JoinAnd(newJQL);
                    newJQL += $"status = \"{Status}\"";
                }
                if(!string.IsNullOrEmpty(Project))
                {
                    newJQL = JoinAnd(newJQL);
                    newJQL += $"project = \"{Project}\"";
                }
                if(!string.IsNullOrEmpty(Type))
                {
                    newJQL = JoinAnd(newJQL);
                    newJQL += $"type = \"{Type}\"";
                }
                if(!string.IsNullOrEmpty(OrderBy))
                {
                    newJQL += $" ORDER BY \"{OrderBy}\"";
                    if(!string.IsNullOrEmpty(Order))
                    {
                        newJQL += $" {Order}";
                    }
                }
                return newJQL;
            }

            string JoinAnd(string input)
            {
                if(!string.IsNullOrEmpty(input))
                {
                    return input + " AND ";
                }
                return input;
            }
        }
    }
}
