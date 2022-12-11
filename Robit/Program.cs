using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace Robit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static DiscordClient botClient;

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
            //Storing the token as a seperate file seemed like a good idea
            StreamReader reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\token.txt");

            string token = reader.ReadToEnd();

            reader.Close();

            Process.Start(lavaLinkStartInfo); //Starts LavaLink and then waits 5 seconds so it has time to start

            Thread.Sleep(5000);

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildVoiceStates,

                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
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

            await Task.Delay(-1);
        }

        private static async Task MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot) return;

            string message = messageArgs.Message.Content.ToLower();

            if (message.Contains("hi"))
            {

                if(message.Contains("robit") || messageArgs.MentionedUsers.First() == botClient.CurrentUser)
                {
                    await messageArgs.Message.RespondAsync($"Hi {messageArgs.Author.Mention}, nice meeting you");
                }
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
                Process.GetProcessesByName("java") != null)
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