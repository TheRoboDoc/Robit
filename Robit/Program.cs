﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
        static void Main(string[] args)
        {
            while (true)
            {
                MainAsync().GetAwaiter().GetResult();
            }
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

            //Logging missing directories
            if (dirsMissing.Count != 0)
            {
                string message = "Missing following directories:\n";

                foreach (string dirMissing in dirsMissing)
                {
                    string dirMissingText = char.ToUpper(dirMissing[0]) + dirMissing.Substring(1);

                    message += $"\t\t\t\t\t\t\t{dirMissingText}\n";
                }

                botClient.Logger.LogWarning("{message}", message);
            }

            botClient.Ready += BotClient_Ready;

            //Connecting the discord client
            await botClient.ConnectAsync();

            botClient.Logger.LogInformation("Connected");
            botClient.Logger.LogInformation("Bot is now operational");

            botClient.MessageCreated += Handler.Run;

            botClient.Heartbeated += StatusUpdate;

            sbyte toggle = -1;
            byte count = 0;

            botClient.Zombied += async (sender, e) =>
            {
                if (count <= 4)
                {
                    count++;
                    return;
                }

                await Task.Run(() =>
                {
                    toggle = 0;
                });
            };

            //Prevents the task from ending
            await Task.Delay(toggle);

            botClient.Logger.LogWarning("RESTARTING DUE TO ZOMBIENG");
        }

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

            string chosenStatus;

            try
            {
                chosenStatus = statuses.ElementAt(random.Next(statuses.Length));
            }
            catch
            {
                botClient?.Logger.LogWarning("Failed to assigne status, defaulting");
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
            botClient?.Logger.LogInformation("Client is ready");

            return Task.CompletedTask;
        }
    }
}