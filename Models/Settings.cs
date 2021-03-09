using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgileBot.Models
{
    [Serializable]
    public class Settings
    {
        [JsonPropertyName("discord")]
        public DiscordSettings Discord { get; set; } = default!;
        [JsonPropertyName("atlassian")]
        public AtlassianSettings Atlassian { get; set; } = default!;
        public override string ToString() => JsonSerializer.Serialize(this);
    }

    [Serializable]
    public class DiscordSettings
    {
        [JsonPropertyName("bot_token")]
        public string? BotToken { get; set; }
        [JsonPropertyName("command_prefix")]
        public string CommandPrefix { get; set; } = "!";
        [JsonPropertyName("arg_seperator_char")]
        public char ArgSeperatorChar { get; set; } = ' ';
        [JsonPropertyName("case_sensitive_commands")]
        public bool CaseSensitiveCommands { get; set; } = false;
        [JsonPropertyName("log_not_found")]
        public bool LogNotFound { get; set; } = false;
        [JsonPropertyName("user_map")]
        public ArgumentMap UserMap { get; set; } = default!;
        [JsonPropertyName("status_map")]
        public ArgumentMap StatusMap { get; set; } = default!;
        public override string ToString() => JsonSerializer.Serialize(this);
    }

    [Serializable]
    public class AtlassianSettings
    {
        [JsonPropertyName("jira_url")]
        public string? JiraURL { get; set; }
        [JsonPropertyName("consumer_key")]
        public string? ConsumerKey { get; set; }
        [JsonPropertyName("consumer_secret")]
        public string? ConsumerSecret { get; set; }
        [JsonPropertyName("oauth_token")]
        public string? OAuthAccessToken { get; set; }
        [JsonPropertyName("oauth_secret")]
        public string? OAuthAccessSecret { get; set; }

        public override string ToString() => JsonSerializer.Serialize(this);
    }

    [Serializable]
    public class ArgumentMap : Dictionary<string, string> { }
}
