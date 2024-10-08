﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using GiphyDotNet.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Managers;
using Robit.Command;
using Robit.Response;
using Robit.TextAdventure;
using System.Diagnostics;

namespace Robit
{
    public class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Discord client
        /// </summary>
        public static DiscordClient? BotClient { get; private set; }

        /// <summary>
        ///     OpenAI service
        /// </summary>
        public static OpenAIService? OpenAiService { get; private set; }

        /// <summary>
        ///     Giphy client
        /// </summary>
        public static Giphy? GiphyClient { get; private set; }

        /// <summary>
        ///     Game manager container containing all the text based adventure game instances managed by the bot
        /// </summary>
        public static GameManagerContainer? GameManagerContainer { get; private set; }

        /// <summary>
        ///     Main Thread
        /// </summary>
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
                DiscordIntents.GuildMessages |
                DiscordIntents.GuildMembers |
                DiscordIntents.GuildVoiceStates,

                MinimumLogLevel = logLevel,
                LogUnknownEvents = DebugStatus(),

                LogTimestampFormat = "dd.MM.yyyy HH:mm:ss (zzz)",
            };

            BotClient = new DiscordClient(config);
            #endregion

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Setting up interactivity...");

            BotClient.UseInteractivity(new InteractivityConfiguration());

            ServiceProvider services = new ServiceCollection()
                .BuildServiceProvider();

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Registering slash commands...");

            #region Command setup
            SlashCommandsConfiguration slashCommandConfig = new SlashCommandsConfiguration()
            {
                Services = services
            };

            SlashCommandsExtension slashCommands = BotClient.UseSlashCommands();

            slashCommands.RegisterCommands<SlashCommands>();

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Slash commands registered");

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Interactivity setup complete");

            #endregion

            List<string> dirsMissing = FileManager.DirCheck().Result.ToList();

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Checking for missing directories...");

            //Logging missing directories
            if (dirsMissing.Count != 0)
            {
                string message = "Missing following directories:\n";

                foreach (string dirMissing in dirsMissing)
                {
                    string dirMissingText = char.ToUpper(dirMissing[0]) + dirMissing.Substring(1);

                    message += $"\t\t\t\t\t\t\t\t{dirMissingText}\n";
                }

                BotClient.Logger.LogWarning(LoggerEvents.Startup, "{message}", message);
            }
            else
            {
                BotClient.Logger.LogInformation(LoggerEvents.Startup, "No missing directories");
            }

            BotClient.SessionCreated += BotClient_Ready;

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Connecting to Discord...");

            //Connecting the discord client
            await BotClient.ConnectAsync();

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Connected");

            BotClient.MessageCreated += Handler.Run;

            BotClient.Heartbeated += StatusUpdate;

            GameManagerContainer = new GameManagerContainer();

            BotClient.GuildMemberAdded += GuildMemberAdded;

            BotClient.Logger.LogInformation(LoggerEvents.Startup, "Bot is now operational");

            BotClient.UseVoiceNext();

            //Prevents the task from ending
            await Task.Delay(-1);
        }

        /// <summary>
        ///     Logic for what to do when a user joins a server
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Discord client that triggered this event
        /// </param>
        /// 
        /// <param name="args">
        ///     Event arguments
        /// </param>
        private static async Task GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            if (!args.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageRoles))
            {
                return;
            }

            await Role.Auto.GiveRole(args.Guild, args.Member);
        }

        public static string? ChosenStatus;

        /// <summary>
        ///     Updates the bots status to a random predetermined value. 
        ///     This is called on hearthbeat event, thus requiring heartbeat event arguments
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Discord client of the bot
        /// </param>
        /// 
        /// <param name="e">
        ///     Heartbeat event's arguments
        /// </param>
        private static async Task StatusUpdate(DiscordClient sender, HeartbeatEventArgs? e = null)
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
                ChosenStatus = statuses.ElementAt(random.Next(statuses.Length));
            }
            catch
            {
                BotClient?.Logger.LogWarning(LoggerEvents.Misc, "Failed to assigne status, defaulting");
                ChosenStatus = statuses.ElementAt(0);
            }

            DiscordActivity activity = new DiscordActivity(ChosenStatus, ActivityType.Custom);

            await sender.UpdateStatusAsync(activity);

            BotClient?.Logger.LogInformation(LoggerEvents.SessionUpdate, "Updating status to {status}", ChosenStatus);
        }

        /// <summary>
        ///     Checks if the bot is running in a debug enviroment
        /// </summary>
        /// 
        /// <returns>
        ///     <list type="bullet">
        ///         <item>
        ///             <c>True</c>: In debug
        ///         </item>
        /// 
        ///         <item>
        ///             <c>False</c>: Not in debug
        ///         </item>
        ///     </list>
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
        ///     What happens once the client is ready
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Client that triggered this task
        /// </param>
        /// 
        /// <param name="e">
        ///     Ready event arguments arguments
        /// </param>
        /// 
        /// <returns>
        ///     The completed task
        /// </returns>
        private static Task BotClient_Ready(DiscordClient sender, SessionReadyEventArgs e)
        {
            BotClient?.Logger.LogInformation(LoggerEvents.Startup, "Client is ready");

            StatusUpdate(sender).GetAwaiter().GetResult();

            return Task.CompletedTask;
        }
    }
}