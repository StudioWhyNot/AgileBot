using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgileBot.Models;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

/// <summary>
/// Discord.Net: https://docs.stillu.cc/index.html
/// Atlassian.SDK: https://bitbucket.org/farmas/atlassian.net-sdk/src/master/
/// Jira OAuth CLI: https://bitbucket.org/farmas/atlassian.net-jira-oauth-cli/src/master/
/// Atlassian API: https://docs.atlassian.com/software/jira/docs/api/REST/latest/
/// Discord embed layout: https://stackoverflow.com/questions/43875943/how-to-use-embedding-with-c-discord-bot
/// </summary>
namespace AgileBot
{
    public class AgileBot
    {
        public const string c_settingsFileName = "settings.json";

        public static AtlassianClient Atlassian { get; private set; }
        public static DiscordSocketClient Discord { get; private set; }
        public static CommandService Commands { get; private set; }

        public static DiscordSettings DiscordSettings { get; private set; } = default!;

        public static async Task Main(string[] args)
        {
            //Load and parse settings .json file.
            var settingsPath = Path.Combine(AppContext.BaseDirectory, c_settingsFileName);
            if(!File.Exists(settingsPath))
            {
                Console.Error.WriteLine($"Could not find {c_settingsFileName} file next to executable.");
                return;
            }
            var settingsJson = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<Settings>(settingsJson);
            
            List<Task> startupTasks = new List<Task>();
            startupTasks.Add(ConnectDiscord(settings.Discord));

            ConnectAtlassian(settings.Atlassian);
            //Wait for async startup tasks.
            foreach(var task in startupTasks)
            {
                await task;
            }
            startupTasks.Clear();
            
            await Task.Delay(-1);
        }

        static void ConnectAtlassian(AtlassianSettings settings)
        {
            Atlassian = new AtlassianClient(settings);
        }

        static async Task ConnectDiscord(DiscordSettings settings)
        {
            DiscordSettings = settings;
            var config = new DiscordSocketConfig();
            
            Discord = new DiscordSocketClient(config);
            
            await Discord.LoginAsync(TokenType.Bot, settings.BotToken);
            Discord.MessageReceived += OnMessageCreated;
            await Discord.StartAsync();

            var commandConfig = new CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = DiscordSettings.CaseSensitiveCommands;
            commandConfig.SeparatorChar = DiscordSettings.ArgSeperatorChar;
            Commands = new CommandService(commandConfig);
            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        public static bool IsOnline(SocketUser user)
        {
            if(user.Status == UserStatus.Online || user.Status == UserStatus.DoNotDisturb) return true;
            return false;
        }

        private static async Task OnMessageCreated(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message is null) return;
            if (message.Author.IsBot) return;

            var content = message.Content.ToLower();
            var commandStart = DiscordSettings.CommandPrefix;
            List<string> commands = new List<string>();
            string subCommand = content;

            //Seperate multiple commands
            int startIndex = GetNextAfterWhitespace(subCommand, commandStart);
            if(startIndex != -1)
            {
                subCommand = subCommand.Substring(startIndex);
                startIndex = 0;
                while(startIndex != -1)
                {
                    if(subCommand.Length <= commandStart.Length) break;
                    var endIndex = GetNextAfterWhitespace(subCommand, commandStart, commandStart.Length);
                    if(endIndex == -1)
                    {
                        commands.Add(subCommand.Substring(commandStart.Length));
                        break;
                    }
                    commands.Add(subCommand.Substring(startIndex + commandStart.Length, endIndex));
                    subCommand = subCommand.Substring(endIndex);
                }
            }

            foreach(var command in commands)
            {
                var parts = command.Split(DiscordSettings.ArgSeperatorChar);
                if(!await Atlassian.TryExecuteJiraCommand(parts, message))
                {
                    var context = new SocketCommandContext(Discord, message);
                    await Commands.ExecuteAsync(context, command, null, MultiMatchHandling.Best);
                    if(DiscordSettings.LogNotFound)
                    {
                        await messageParam.Channel.SendMessageAsync($"Failed to find task or command: {command}");
                    }
                }
            }
        }

        static int GetNextAfterWhitespace(string me, string find, int startIndex = 0)
        {
            int index = me.IndexOf(find, startIndex);
            while(index > 0 && !char.IsWhiteSpace(me[index - 1]))
            {
                index = me.IndexOf(find, index + find.Length);
            }
            return index;
        }
    }
}
