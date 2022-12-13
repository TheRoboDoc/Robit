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
using OpenAI.GPT3.Interfaces;
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
            StreamReader reader1 = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\OpenAIToken.txt");

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = reader1.ReadToEnd()
            });

            reader1.Close();

            //Storing the token as a seperate file seemed like a good idea
            StreamReader reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\token.txt");

            string token = reader.ReadToEnd();

            reader.Close();

            Process.Start(lavaLinkStartInfo); //Starts LavaLink and then waits 10 seconds so it has time to start

            Thread.Sleep(10000);

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildVoiceStates,
                MinimumLogLevel = LogLevel.Information,

                LogTimestampFormat = "dd.mm.yyyy hh:mm:ss"           
            };

            botClient = new DiscordClient(config);

            ServiceProvider services = new ServiceCollection()
                .BuildServiceProvider();

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

            botClient.Ready += BotClient_Ready;

            LavalinkExtension lavalink = botClient.UseLavalink();

            await botClient.ConnectAsync();
            botClient.Logger.LogInformation("Connected");
            botClient.Logger.LogInformation("Connetinng to local LavaLink server...");
            await lavalink.ConnectAsync(lavalinkConfig);
            botClient.Logger.LogInformation("LavaLink connected");

            lavalink.NodeDisconnected += Lavalink_NodeDisconnected;

            botClient.MessageCreated += MessageCreated;

            
            DiscordActivity activity;
            UserStatus userStatus;

            if (Debugger.IsAttached)
            {
                activity = new DiscordActivity()
                {
                    ActivityType = ActivityType.Playing,
                    Name = "Debug Mode",
                };

                userStatus = UserStatus.DoNotDisturb;
            }
            else
            {
                activity = new DiscordActivity()
                {
                    ActivityType = ActivityType.Playing,
                    Name = "Vibing",
                };

                userStatus = UserStatus.Online;
            }
            

            await botClient.UpdateStatusAsync(activity, userStatus, DateTimeOffset.Now);

            Process.GetProcesses().FirstOrDefault().Exited += ProcessExit;

            await Task.Delay(-1);
        }

        private static void ProcessExit(object? sender, EventArgs e)
        {
            try
            {
                Process.GetProcesses("java.exe").First().Kill();
                Process.GetProcesses("powershell.exe").First().Kill();
            }
            catch
            {
                botClient.Logger.LogWarning("Failed to close LavaLink");
            }
        }

        private static async Task MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return;

            string[] keyTerms = { "elf", "elves", "furry", "furries", "miku" };
            double index = double.NaN;

            string messageLower = messageArgs.Message.Content.ToLower();

            foreach(string keyTerm in keyTerms)
            {
                if (messageLower.Contains(keyTerm))
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
            }

            int mentiones = 0;

            foreach(var mentionedUser in messageArgs.MentionedUsers)
            {
                if (mentionedUser == botClient.CurrentUser) mentiones++;
            }

            if (mentiones == 0) return;

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
                var reply = await messageArgs.Message.RespondAsync("Thinking");

                string messageReply = reply.Content;

                for(int i = 0; i <= 2; i++)
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
        }

        private static Task BotClient_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            botClient.Logger.LogInformation("Ready");

            return Task.CompletedTask;
        }

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