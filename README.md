# Agile Bot
Agile bot is a free and open source integration between Atlassian Jira and Discord. Support for other Agile and Kanban platforms may appear in the future, but for now it is only Jira.

**⚠️This app is currently in a *very* early state, use it at your own risk!⚠️**
## Usage Examples
Simply run a command with the key for a project or issue in order to have the bot embed it.!

<br><img src="https://user-images.githubusercontent.com/8867456/149078972-9eb41d83-8d21-4010-ba05-bb2de1be7751.png" alt="embedding project" width="500"/>

Multiple commands in a single line are supported.!

<br><img src="https://user-images.githubusercontent.com/8867456/149079016-80d34611-acff-4fc9-970c-d6493c645e15.png" alt="embedding issues" width="300"/>

As are groups of tasks.

<br><img src="https://user-images.githubusercontent.com/8867456/149079050-9d3f92ab-2782-49ac-b710-ba123e79be26.png" alt="embedding issues" width="340"/>

Custom JQL and other advanced Queries are also available. For examples, see **Property Overrides** below.

## Property Overrides
Each property of TaskParams in Commands.cs can be overridden using the [syntax](https://discordnet.dev/guides/text_commands/namedarguments.html) defined by Discord.NET. The names of properties are _not_ case-sensitive. Arguments with spaces can be surrounded by quotes.

The fields are as follows:
> **Max**: The maximum number of tasks to display. Defaults to 10.
> 
> **User**: Search for tasks with this user as an assignee. Discord names will be mapped to Jira names as specified in the `settings.json`. Defaults to the querying user.
> 
> **Project**: The project to search. No default.
> 
> **Type**: The type of Jira story to search. Default changes depending on whether `!tasks` or `!bugs` was called.
> 
> **Status**: The status of the tasks to search for. Defaults to "In Progress" as specified in the `settings.json` if `!tasks` was called or "Bug" if `!bugs` was called.
> 
> **OrderBy**: How the tasks should be ordered in the Discord embed. Defaults to "priority".
> 
> **Order**: How the ordering should be displayed in the Discord embed. Defaults to "desc" (Descending).
> 
> **PreJql**: A JQL statement that is prepended to the other fields with an "AND" statment. Can be used to combine JQL and the other fields. No default.
> 
> **Jql**: A raw JQL statment that will cause any other fields to be ignored. No default.

An example of Property Overrides:

`!tasks project:PWS status: "In Progress" User: Scion`

An example of a PreJql query:

`!tasks PWS Done OrderBy: created PreJql: "createdDate <=  '2021/06/30' AND createdDate >=  '2021/06/29'"`

An example of a Jql query:

`!tasks jql: "status = Done AND assignee = scion AND createdDate <=  '2021/06/30' AND createdDate >=  '2021/06/29' ORDER BY priority DESC"`

## Running the bot
Simply download and build the project for your desired hosting platform and run the application next to a `settings.json` file. `settings.json.example` provides an example.

### Adding a Bot to your Discord Server
1. Go to: https://discord.com/developers/applications
2. Create a new application.
3. Activate: Settings->Bot
4. Reveal the Token and copy it to the "bot_token" setting.
5. Under OAuth2, select the "bot" Scope and copy the URL. **DO NOT SHARE THIS TOKEN!**
6. Visit the URL in your browser and Authorize the bot to join your desired server.

### Authorizing the Bot with Jira
Install the [.NET SDK](https://dotnet.microsoft.com/download/ ".NET SDK") for the dotnet command line tool if you have not already.
Now you need to use the [Jira OAuth CLI](https://bitbucket.org/farmas/atlassian.net-jira-oauth-cli/src/master/ "Jira OAuth CLI") tool to authorize the application to your Jira instance.
1. Run `dotnet tool install -g jira-oauth-cli`
2. Run `jira-oauth-cli get-keys`
3. Visit your Jira instance's Application links, enter whatever you think is appropriate for this first form but select **'Generate incoming link'** and click next.
	- Application links can be found under `Settings->Products->Application links` in Jira Cloud and `Settings->Applications->Application links` in Jira Server/Data Center.
4. Input a custom `Consumer Key` and remember it.
5. Paste in the `Public Key` from Step 2.
6. Run `jira-oauth-cli get-tokens --url <Your_Atlassian_Root_URL> -u <Username> -p <Password> -k <Step4_Consumer_Key> -s <Step2_Private_Key>`
7. Visit the output URL in your browser and authorize the app.
8. In settings.json set the following from the output:
```json
"Consumer Key:" -> "consumer_key"
"Consumer Secret:" -> "consumer_secret"
"Access Token:" -> "oauth_token"
"Token Secret:" -> "oauth_secret"
```
**DO NOT SHARE ANY OF THESE TOKENS!**

Now just run the bot and, if all goes well, you should see it appear as active in your Discord server. Happy sprinting!

## Running the bot in a docker container

For convenience, create a `.env` file and set it up with all the tokens from before. Like so:
```
BOT_TOKEN=<ex: Discord Bot token>
JIRA_URL=<ex: https://myjirainstance.atlassian.net/>
CONSUMER_KEY=<ex: Jira_Bot>
CONSUMER_SECRET=<ex: Massive string>
OAUTH_TOKEN=<ex: Token string>
OAUTH_SECRET=<ex: Secret string>
```
Then modify `settings.json` to your needs (notice the environment variables names being used correspond with the ones in `.env`), you can use docker compose to bring up the container.
```bash
docker-compose up -d --build
```
You could also build the image.
```bash
docker build . -t agile-bot
```
And then run it.
```bash
docker run -d \
--name=agile-bot \
--restart unless-stopped \
--env-file ./.env \
agile-bot
```
