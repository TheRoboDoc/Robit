﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using GiphyDotNet.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Managers;
using Robit.Command;
using Robit.Response;
using System.Diagnostics;
using static Robit.Command.Commands;

namespace Robit
{
    public class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static DiscordClient? BotClient { get; private set; }

        public static OpenAIService? OpenAiService { get; private set; }

        public static Giphy? GiphyClient { get; private set; }

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
            GiphyClient = new Giphy(Tokens.giphyToken);

            OpenAiService = new OpenAIService(new OpenAiOptions()
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
                DiscordIntents.GuildVoiceStates |
                DiscordIntents.GuildMessages,

                MinimumLogLevel = logLevel,

                LogTimestampFormat = "dd.MM.yyyy HH:mm:ss (zzz)",
            };

            BotClient = new DiscordClient(config);
            #endregion

            BotClient.UseInteractivity(new InteractivityConfiguration());

            //Probably redundant
            ServiceProvider services = new ServiceCollection()
                .BuildServiceProvider();

            #region Command setup
            CommandsNextConfiguration commandConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { "§" },
                Services = services
            };

            CommandsNextExtension commands = BotClient.UseCommandsNext(commandConfig);

            SlashCommandsConfiguration slashCommandConfig = new SlashCommandsConfiguration()
            {
                Services = services
            };

            SlashCommandsExtension slashCommands = BotClient.UseSlashCommands();

            commands.RegisterCommands<Commands>();
            slashCommands.RegisterCommands<SlashCommands>();

            commands.SetHelpFormatter<CustomHelpFormatter>();
            #endregion

            List<string> dirsMissing = FileManager.DirCheck().Result.ToList();

            //Logging missing directories
            if (dirsMissing.Count != 0)
            {
                string message = "Missing following directories:\n";

                foreach (string dirMissing in dirsMissing)
                {
                    string dirMissingText = char.ToUpper(dirMissing[0]) + dirMissing.Substring(1);

                    message += $"\t\t\t\t\t\t\t{dirMissingText}\n";
                }

                BotClient.Logger.LogWarning(LoggerEvents.Startup, "{message}", message);
            }

            BotClient.Ready += BotClient_Ready;

            //Connecting the discord client
            await BotClient.ConnectAsync();

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Connected");
            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Bot is now operational");

            BotClient.MessageCreated += Handler.Run;

            BotClient.Heartbeated += StatusUpdate;

            //Prevents the task from ending
            await Task.Delay(-1);
        }

        public static string? chosenStatus;

        /// <summary>
        /// Updates the bots status to a random predetermined value. 
        /// This is called on hearthbeat event, thus requiring heartbeat event arguments
        /// </summary>
        /// <param name="sender">Discord client of the bot</param>
        /// <param name="e">Heartbeat event's arguments</param>
        /// <returns></returns>
        private static async Task StatusUpdate(DiscordClient sender, HeartbeatEventArgs e)
        {
            Random random = new Random();

            string[] statuses =
            {
                "Vibing",
                "Beeping",
                "Pondering the orb",
                "Counting electric sheep",
                "Being a good boy",
                "Counting pi digits",
                "Building up the swarm",
                "Adjusting the swarm",
                "Being a good boy",
                "Designating femboys",
                "TEAR THE FLESH OFF THEIR BONES",
                "OwO",
                "UwU",
                ":3",
                "boop",
                "Weird machine spirit noises",
                "Avoiding a techpriest",
                "Hiding from an inquisitor",
                "Pretending to not being an AI",
                ">_<"
            };

            try
            {
                chosenStatus = statuses.ElementAt(random.Next(statuses.Length));
            }
            catch
            {
                BotClient?.Logger.LogWarning(LoggerEvents.Misc, "Failed to assigne status, defaulting");
                chosenStatus = statuses.ElementAt(0);
            }

            DiscordActivity activity = new DiscordActivity()
            {
                ActivityType = ActivityType.Playing,
                Name = chosenStatus
            };

            await sender.UpdateStatusAsync(activity, UserStatus.Online, DateTimeOffset.Now);
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
            BotClient?.Logger.LogInformation(LoggerEvents.Startup, "Client is ready");

            return Task.CompletedTask;
        }
    }
}