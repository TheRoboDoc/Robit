using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using DSharpPlus.Entities;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Robit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static DiscordClient botClient;

        public  static OpenAIService openAiService;

        /// <summary>
        /// Defines start information for Lavalink
        /// </summary>
        public static ProcessStartInfo lavaLinkStartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $@"cd {AppDomain.CurrentDomain.BaseDirectory}; java -jar Lavalink.jar",
            CreateNoWindow = false,
            UseShellExecute = true
        };

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
            #region OpenAI Client setup
            StreamReader reader1 = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\OpenAIToken.txt");

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = reader1.ReadToEnd()
            });

            reader1.Close();
            #endregion

            #region Discord Client setup
            string tokenFileLocation;

            LogLevel logLevel;

            if (DebugStatus())
            {
                tokenFileLocation = AppDomain.CurrentDomain.BaseDirectory + @"\debugToken.txt";
                logLevel = LogLevel.Debug;
            }
            else
            {
                tokenFileLocation = AppDomain.CurrentDomain.BaseDirectory + @"\token.txt";
                logLevel = LogLevel.Information;
            }

            //Storing the token as a seperate file seemed like a good idea
            StreamReader reader = new StreamReader(tokenFileLocation);

            string token = reader.ReadToEnd();

            reader.Close();

            int lavalinkProcessIdProcess = Process.Start(lavaLinkStartInfo).Id; //Starts LavaLink and then waits 10 seconds so it has time to start

            

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildVoiceStates,
                MinimumLogLevel = logLevel,

                LogTimestampFormat = "dd.MM.yyyy HH:mm:ss (zzz)"           
            };

            botClient = new DiscordClient(config);
            #endregion

            //Probably redundant
            ServiceProvider services = new ServiceCollection()
                .BuildServiceProvider();

            #region Command setup
            CommandsNextConfiguration commandConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { "§" },
                Services = services
            };

            CommandsNextExtension commands = botClient.UseCommandsNext(commandConfig);

            SlashCommandsConfiguration slashCommandConfig = new SlashCommandsConfiguration()
            {
                Services = services
            };

            SlashCommandsExtension slashCommands = botClient.UseSlashCommands();

            commands.RegisterCommands<Commands>();
            slashCommands.RegisterCommands<SlashCommands>();

            commands.SetHelpFormatter<CustomHelpFormatter>();
            #endregion

            #region Lavalink setup
            ConnectionEndpoint endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1",
                Port = 2333
            };

            LavalinkConfiguration lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass",
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            LavalinkExtension lavalink = botClient.UseLavalink();
            #endregion

            botClient.Ready += BotClient_Ready;

            //Connecting the discord client
            await botClient.ConnectAsync();

            botClient.Logger.LogInformation("Connected");
            botClient.Logger.LogInformation("Connetinng to local LavaLink server...");

            Thread.Sleep(10000); //Waiting for LavaLink to be fully ready

            //Connecting to the running LavaLink instance
            await lavalink.ConnectAsync(lavalinkConfig);
            botClient.Logger.LogInformation("LavaLink connected");
            botClient.Logger.LogInformation("Bot is now operational");

            lavalink.NodeDisconnected += Lavalink_NodeDisconnected;

            botClient.MessageCreated += Response;
            botClient.MessageCreated += AIResponse;

            //This doesn't work, why???
            Process.GetCurrentProcess().Exited += (s, e) =>
            {
                try
                {
                    Process.GetProcessById(lavalinkProcessIdProcess).Kill();
                }
                catch
                {
                    botClient.Logger.LogWarning("Failed to close LavaLink");
                }
            };

            //The bot has a tendency to lose it's current activity if it gets disconnected
            //This is why we periodically (on every heartbeat) set it to the correct one
            botClient.Heartbeated += async (client, e) =>
            {
                DiscordActivity activity;
                string activityText = "Vibing";

                //If the activity is already what we want it to be then we do nothing
                if (client.CurrentUser.Presence.Activity.Name == activityText) return;

                activity = new DiscordActivity()
                {
                    ActivityType = ActivityType.Playing,
                    Name = activityText
                };

                await client.UpdateStatusAsync(activity, UserStatus.Online, DateTimeOffset.Now);

                botClient.Logger.LogInformation("Had to set activity");
            };

            //Prevents the task from ending
            await Task.Delay(-1);
        }

        /// <summary>
        /// Checks if the bot is running in a debug enviroment
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><c>True</c>: In debug</item>
        /// <item><c>False</c>: Not in debug</item>
        /// </list>
        /// </returns>
        private static bool DebugStatus()
        {
            bool debugState;

            if(Debugger.IsAttached)
            {
                debugState = true;
            }
            else
            {
                debugState = false;
            }

            return debugState;
        }

        /// <summary>
        /// Responses to messages that contain certain key terms
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        private static async Task Response(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return;

            string[] keyTerms = 
            { 
                "elf", 
                "elves", 
                "furry", 
                "furries", 
                "miku",
                "same"
            };

            double index = double.NaN;

            string messageLower = messageArgs.Message.Content.ToLower();

            string[] wordsInMessage = messageLower.Split(' ');

            foreach(string keyTerm in keyTerms)
            {
                if (wordsInMessage.Contains(keyTerm))
                {
                    index = Array.IndexOf(keyTerms, keyTerm);
                    break;
                }
            }

            switch (index)
            {
                //Elf
                case 0:
                case 1:
                    await messageArgs.Message.RespondAsync(@"https://cdn.discordapp.com/attachments/884936240321929280/1051151859013931078/344171626528768012.jpg");
                    break;
                //Furry
                case 2:
                case 3:
                    await messageArgs.Message.RespondAsync(@"https://cdn.discordapp.com/attachments/884936240321929280/1051316162383851581/324047254149267459.png");
                    break;
                //Miku
                case 4:
                    await messageArgs.Message.RespondAsync(@"https://pbs.twimg.com/media/E3KD4WpUUAAlciY?format=jpg&name=4096x4096");
                    break;
                //Same
                case 5:
                    await messageArgs.Message.RespondAsync(@":shark:");
                    break;
            }
        }

        /// <summary>
        /// AI responses when prompted
        /// </summary>
        /// <param name="sender">Discord client that triggered this task</param>
        /// <param name="messageArgs">Message creation event arguments</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Task AIResponse(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            //Run as a task because otherwise get a warning that event handler for Message created took too long
            _ = Task.Run(async () =>
            {
                uint mentiones = 0;

                foreach (var mentionedUser in messageArgs.MentionedUsers)
                { 
                    if (mentionedUser == botClient.CurrentUser) mentiones++;
                }

                if (mentiones == 0) return;

                var reply = await messageArgs.Message.RespondAsync("Thinking");

                CompletionCreateResponse completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
                {
                    Prompt = "Robit is a simple Discord Bot made by RoboDoc that can play music and answer simple questions.\n" +
                    "He isn't very sophisticated and cannot have full blown conversations.\n" +
                    "Just simple replies to questions. Those replies have maximum lengh of 100 characters\n\n" +
                    $"{messageArgs.Author.Username}#{messageArgs.Author.Discriminator}: {messageArgs.Message.Content}\n" +
                    $"Robit:",
                    MaxTokens = 30,
                    Temperature = 0.3F,
                    TopP = 0.3F,
                    PresencePenalty = 0,
                    FrequencyPenalty = 0.5F
                }, Models.TextDavinciV3);

                if (completionResult.Successful)
                {
                    string messageReply = reply.Content;

                    for (int i = 0; i <= 2; i++)
                    {
                        messageReply += ".";
                        await reply.ModifyAsync(messageReply);
                        Thread.Sleep(500);
                    }

                    await reply.DeleteAsync();

                    await messageArgs.Channel.SendMessageAsync(completionResult.Choices[0].Text);
                    botClient.Logger.LogInformation(completionResult.Choices[0].Text);
                }
                else
                {
                    if (completionResult.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }
                    botClient.Logger.LogError($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// What happens once the client is ready
        /// </summary>
        /// <param name="sender">Client that triggered this task</param>
        /// <param name="e">Ready event arguments arguments</param>
        /// <returns>The completed task</returns>
        private static Task BotClient_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            botClient.Logger.LogInformation("Client is ready");

            return Task.CompletedTask;
        }

        /// <summary>
        /// What happens when LavaLink node disconnects
        /// </summary>
        /// <param name="sender">Node connection that triggered this task</param>
        /// <param name="e">Disconnection arguments</param>
        private static async Task Lavalink_NodeDisconnected(LavalinkNodeConnection sender, NodeDisconnectedEventArgs e)
        {

            botClient.Logger.LogCritical("LavaLink disconnected");
            await Task.Run(() =>
            {
                Process.Start(lavaLinkStartInfo);
                botClient.Logger.LogInformation("Attempting to start LavaLink...");
                Thread.Sleep(5000);
            });

            if(Process.GetProcessesByName("powershell.exe") != null &&
                Process.GetProcessesByName("java.exe") != null)
            {
                botClient.Logger.LogInformation("LavaLink started successfully");
            }
            else
            {
                botClient.Logger.LogCritical("Failed to start LavaLink");
            }
        }
    }
}