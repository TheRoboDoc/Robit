using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
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

        public static ProcessStartInfo lavaLinkStartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $@"cd {AppDomain.CurrentDomain.BaseDirectory}; java -jar Lavalink.jar",
            CreateNoWindow = false,
            UseShellExecute = true
        };

        static async Task MainAsync()
        {
            StreamReader reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\token.txt");

            string token = reader.ReadToEnd();

            reader.Close();

            Process.Start(lavaLinkStartInfo);

            Thread.Sleep(5000);

            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.Guilds |
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildVoiceStates,

                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
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

            slashCommands.RegisterCommands<SlashCommands>(); //669902122992533506
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

            LavalinkExtension lavalink = botClient.UseLavalink();

            await botClient.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            lavalink.NodeDisconnected += Lavalink_NodeDisconnected; ;

            await Task.Delay(-1);
        }

        private static async Task Lavalink_NodeDisconnected(LavalinkNodeConnection sender, NodeDisconnectedEventArgs e)
        {
            await Task.Run(() =>
            {
                Process.Start(lavaLinkStartInfo);
            });

            throw new NotImplementedException();
        }
    }
}