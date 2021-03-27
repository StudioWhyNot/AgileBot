# Agile Bot
Agile bot is a free and open source integration between Atlassian Jira and Discord. Support for other Agile and Kanban platforms may appear in the future, but for now it is only Jira.

**⚠️This app is currently in a *very* early state, use it at your own risk!⚠️**
## Usage Examples
Simply run a command with the key for a project or issue in order to have the bot embed it.
<br><img src="https://bit.ly/2N4c4D4" alt="embedding project" width="500"/>

Multiple commands in a single line are supported.
<br><img src="https://bit.ly/3qvB9Ek" alt="embedding issues" width="300"/>

As are groups of tasks.
<br><img src="https://bit.ly/3cdhvId" alt="embedding issues" width="340"/>

Custom JQL and other advanced Queries are also available. For now, you can see them in Commands.cs.
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