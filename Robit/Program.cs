using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using GiphyDotNet.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using Robit.Command;
using Robit.Response;
using System.Diagnostics;
using static Robit.Command.Commands;

namespace Robit
{
    public class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static DiscordClient? botClient;

        public static OpenAIService? openAiService;

        public static Giphy? giphyClient;

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
            giphyClient = new Giphy(Tokens.giphyToken);

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Tokens.OpenAIToken
            });

            #region Discord Client setup
            LogLevel logLevel;

            string token;

            if (DebugStatus())
            {
                token = Tokens.debugToken;
                logLevel = LogLevel.Debug;
            }
            else
            {
                token = Tokens.token;
                logLevel = LogLevel.Information;
            }

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.MessageContents |
                DiscordIntents.Guilds |
                DiscordIntents.GuildPresences |
                DiscordIntents.GuildMessages,

                MinimumLogLevel = logLevel,

                LogTimestampFormat = "dd.MM.yyyy HH:mm:ss (zzz)",
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

            List<string> dirsMissing = FileManager.DirCheck().Result.ToList();

            if (dirsMissing.Count != 0)
            {
                string message = "Missing following directories:\n";

                foreach (string dirMissing in dirsMissing)
                {
                    string dirMissingText = char.ToUpper(dirMissing[0]) + dirMissing.Substring(1);

                    message += $"\t\t\t\t\t\t\t{dirMissingText}\n";
                }

                botClient.Logger.LogWarning(message);
            }

            botClient.Ready += BotClient_Ready;

            //Connecting the discord client
            await botClient.ConnectAsync();

            botClient.Logger.LogInformation("Connected");
            botClient.Logger.LogInformation("Bot is now operational");

            botClient.MessageCreated += Handler.Run;

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
        public static bool DebugStatus()
        {
            bool debugState;

            if (Debugger.IsAttached)
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
        /// What happens once the client is ready
        /// </summary>
        /// <param name="sender">Client that triggered this task</param>
        /// <param name="e">Ready event arguments arguments</param>
        /// <returns>The completed task</returns>
        private static Task BotClient_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            botClient?.Logger.LogInformation("Client is ready");

            return Task.CompletedTask;
        }
    }
}