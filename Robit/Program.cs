using DSharpPlus;
using DSharpPlus.CommandsNext;
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
using static Robit.Commands;

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
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
            #region OpenAI Client setup
            StreamReader reader1 = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"/OpenAIToken.txt");

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
                tokenFileLocation = AppDomain.CurrentDomain.BaseDirectory + @"/debugToken.txt";
                logLevel = LogLevel.Debug;
            }
            else
            {
                tokenFileLocation = AppDomain.CurrentDomain.BaseDirectory + @"/token.txt";
                logLevel = LogLevel.Information;
            }

            //Storing the token as a seperate file seemed like a good idea
            StreamReader reader = new StreamReader(tokenFileLocation);

            string token = reader.ReadToEnd();

            reader.Close();

            //Bot config stuff, token, intents etc.
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,

                Intents =
                DiscordIntents.MessageContents |
                DiscordIntents.Guilds |
                DiscordIntents.GuildMessages,
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

            if (!FileManager.ResponseManager.DirCheck().Result)
            {
                botClient.Logger.LogInformation("Had to create Data directory");
            }

            botClient.Ready += BotClient_Ready;

            //Connecting the discord client
            await botClient.ConnectAsync();

            botClient.Logger.LogInformation("Connected");
            botClient.Logger.LogInformation("Bot is now operational");

            botClient.MessageCreated += Response;
            botClient.MessageCreated += AIResponse;

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
        /// Responses to messages that contain trigger content as defined by response interactions for a guild
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        private static async Task Response(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            //This should be made into a command in the future. Where server owner/admins can add responses
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return;

            List<FileManager.ResponseManager.ResponseEntry> responseEntries = new List<FileManager.ResponseManager.ResponseEntry>();

            responseEntries = FileManager.ResponseManager.ReadEntries(messageArgs.Guild.Id.ToString());

            string messageLower = messageArgs.Message.Content.ToLower();

            #region Special characters that are removed
            messageLower = messageLower.Replace("+", " ");
            messageLower = messageLower.Replace("`", " ");
            messageLower = messageLower.Replace("¨", " ");
            messageLower = messageLower.Replace("\'", " ");
            messageLower = messageLower.Replace(",", " ");
            messageLower = messageLower.Replace(".", " ");
            messageLower = messageLower.Replace("-", " ");
            messageLower = messageLower.Replace("!", "" );
            messageLower = messageLower.Replace("\"", " ");
            messageLower = messageLower.Replace("#", " ");
            messageLower = messageLower.Replace("¤", " ");
            messageLower = messageLower.Replace("%", " " );
            messageLower = messageLower.Replace("&", " ");
            messageLower = messageLower.Replace("/", " ");   
            messageLower = messageLower.Replace("(", " ");
            messageLower = messageLower.Replace(")", " ");
            messageLower = messageLower.Replace("=", " ");
            messageLower = messageLower.Replace("?", " ");
            messageLower = messageLower.Replace("´", " ");
            messageLower = messageLower.Replace("^", " ");
            messageLower = messageLower.Replace("*", " ");
            messageLower = messageLower.Replace(";", " ");
            messageLower = messageLower.Replace(":", " ");
            messageLower = messageLower.Replace("_", " ");
            messageLower = messageLower.Replace("§", " ");
            messageLower = messageLower.Replace("½", " ");
            messageLower = messageLower.Replace("@", " ");
            messageLower = messageLower.Replace("£", " ");
            messageLower = messageLower.Replace("$", " ");
            messageLower = messageLower.Replace("€", " ");
            messageLower = messageLower.Replace("{", " ");
            messageLower = messageLower.Replace("[", " ");
            messageLower = messageLower.Replace("]", " ");
            messageLower = messageLower.Replace("}", " ");
            messageLower = messageLower.Replace("\\", " ");
            messageLower = messageLower.Replace("~", " ");
            #endregion

            if (Debugger.IsAttached)
            {
                await messageArgs.Message.RespondAsync(messageLower);
            }
            
            string[] wordsInMessage = messageLower.Split(' ');

            foreach (string word in wordsInMessage)
            {
                foreach(FileManager.ResponseManager.ResponseEntry responseEntry in responseEntries)
                {
                    if(word == responseEntry.content.ToLower())
                    {
                        await messageArgs.Message.RespondAsync(responseEntry.response);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// AI responses when prompted
        /// </summary>
        /// <param name="sender">Discord client that triggered this task</param>
        /// <param name="messageArgs">Message creation event arguments</param>
        /// <returns></returns>
        /// <exception cref="Exception">AI module response fail</exception>
        private static Task AIResponse(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot) return Task.CompletedTask;

            //Run as a task because otherwise get a warning that event handler for Message created took too long
            Task response = Task.Run(async () =>
            {
                bool botMentioned = false;

                foreach (DiscordUser mentionedUser in messageArgs.MentionedUsers)
                { 
                    if (mentionedUser == botClient.CurrentUser)
                    {
                        botMentioned = true;
                        break;
                    }
                }

                if (!botMentioned) return;

                //Required to cancel the task "thinking"
                //Needed as it is an infinite loop
                CancellationTokenSource thinkingCancelTokenSource = new CancellationTokenSource();
                CancellationToken thinkingCancelToken = thinkingCancelTokenSource.Token;

                DiscordMessage reply = await messageArgs.Message.RespondAsync("Thinking");

                //Task that handles animation of "Thinking..." display
                Task thinking = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (thinkingCancelToken.IsCancellationRequested)
                        {
                            return Task.CompletedTask;
                        }

                        await reply.ModifyAsync("Thinking");
                        string messageReply = reply.Content;

                        for (int i = 0; i <= 2; i++)
                        {
                            if (thinkingCancelToken.IsCancellationRequested)
                            {
                                return Task.CompletedTask;
                            }

                            messageReply += ".";
                            await reply.ModifyAsync(messageReply);
                            Thread.Sleep(750);
                        }
                    }

                }, thinkingCancelToken);

                //AI propmt
                CompletionCreateResponse completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
                {
                    Prompt = 
                    $"{messageArgs.Guild.CurrentMember.DisplayName} is a friendly discord bot that tries to answer user questions to the best of his abilities\n" +
                    "He is very passionate, but understands that he cannot answer every questions and tries to avoid " +
                    "answering directly to sensetive topics." +
                    "He isn't very sophisticated and cannot have full blown conversations.\n" +
                    "His responses are generated using OpenAI Davinci V3 text AI model" +
                    "Just simple replies to questions. Those replies have maximum lengh of 100 characters.\n\n" +
                    $"{messageArgs.Author.Username}#{messageArgs.Author.Discriminator}: {messageArgs.Message.Content}\n" +
                    $"{messageArgs.Guild.CurrentMember.DisplayName}:",
                    MaxTokens = 60,
                    Temperature = 0.3F,
                    TopP = 0.3F,
                    PresencePenalty = 0,
                    FrequencyPenalty = 0.5F
                }, Models.TextDavinciV3);

                //If we get a proper result from OpenAI
                if (completionResult.Successful)
                {
                    thinkingCancelTokenSource.Cancel();

                    await reply.ModifyAsync(completionResult.Choices[0].Text);

                    //Log the AI interaction only if we are in debug mode
                    if (DebugStatus())
                    {
                        botClient.Logger.LogDebug($"Message: {messageArgs.Message.Content}");
                        botClient.Logger.LogDebug($"Reply: {completionResult.Choices[0].Text}");
                    }

                    //Double checks as message modify might not actually do it some times
                    if(reply.Content != completionResult.Choices[0].Text)
                    {
                        await reply.ModifyAsync(completionResult.Choices[0].Text);
                    }
                }
                else
                {
                    thinkingCancelTokenSource.Cancel();

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
    }
}