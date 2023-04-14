using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using Robit.Command;
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

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns>Nothing</returns>
        static async Task MainAsync()
        {
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
                DiscordIntents.GuildVoiceStates |
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

            botClient.MessageCreated += Response;
            botClient.MessageCreated += AIResponse;
            botClient.MessageCreated += DiscordNoobFailsafe;

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
        /// A failsafe for when a user tries to execute a slash command but sends it as a plain message instead.
        /// Deletes the failed command message and after 10 seconds deletes the warning message.
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        private static async Task DiscordNoobFailsafe(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return;

            try
            {
                if (messageArgs.Message.Content.First() != '/') return;
            }
            catch
            {
                if (DebugStatus())
                {
                    botClient?.Logger.LogInformation("The message was empty");
                }
            }

            SlashCommandsExtension slashCommandsExtension = botClient.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension.RegisteredCommands;
            List<DiscordApplicationCommand> globalCommands =
                slashCommandsList.Where(x => x.Key == null).SelectMany(x => x.Value).ToList();

            List<string> commands = new List<string>();

            foreach (DiscordApplicationCommand globalCommand in globalCommands)
            {
                commands.Add(globalCommand.Name);
            }

            DiscordMessage? message = null;

            foreach (string command in commands)
            {
                if (messageArgs.Message.Content.Contains(command))
                {
                    await messageArgs.Message.DeleteAsync();

                    message = await messageArgs.Message.RespondAsync
                        ($"{messageArgs.Author.Mention} you tried running a {command} command, but instead send it as a plain message. " +
                        $"That doesn't look very nice for you. So I took the liberty to delete it");

                    break;
                }
            }

            // Delets message
            _ = Task.Run(async () =>
            {
                if (message != null)
                {
                    await Task.Delay(10000);
                    await message.DeleteAsync();
                }
            });
        }

        /// <summary>
        /// Checks if the bot was mentioned in a message
        /// </summary>
        /// <param name="messageArgs">Arguments of the message to check</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><c>True</c>: Mentioned</item>
        /// <item><c>False</c>: Not mentioned</item>
        /// </list>
        /// </returns>
        private static async Task<bool> CheckBotMention(MessageCreateEventArgs messageArgs)
        {
            bool botMentioned = false;

            await Task.Run(() =>
            {
                foreach (DiscordUser mentionedUser in messageArgs.MentionedUsers)
                {
                    if (mentionedUser == botClient?.CurrentUser)
                    {
                        botMentioned = true;
                        break;
                    }
                }
            });

            return botMentioned;
        }

        /// <summary>
        /// Responses to messages that contain trigger content as defined by response interactions for a guild
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        private static async Task Response(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Message.Content == null) return;

            if (messageArgs.Author.IsBot || messageArgs.Equals(null) || CheckBotMention(messageArgs).Result) return;

            List<FileManager.ResponseManager.ResponseEntry> responseEntries = new List<FileManager.ResponseManager.ResponseEntry>();

            responseEntries = FileManager.ResponseManager.ReadEntries(messageArgs.Guild.Id.ToString());

            string messageLower = messageArgs.Message.Content.ToLower();

            messageLower = WordFilter.WordFilter.SpecialCharacterRemoval(messageLower);

            string[] wordsInMessage = messageLower.Split(' ');

            foreach (string word in wordsInMessage)
            {
                foreach (FileManager.ResponseManager.ResponseEntry responseEntry in responseEntries)
                {
                    if (word == responseEntry.content.ToLower())
                    {
                        await messageArgs.Message.RespondAsync(responseEntry.response);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a discord message with the RobitThink.gif animation
        /// </summary>
        /// <returns>DiscordMessageBuilder</returns>
        public static async Task<DiscordMessageBuilder> MessageThinkingAnimation()
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();

            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RobitThink.gif");

                builder.AddFile(fileStream);
            });

            return builder;
        }

        /// <summary>
        /// Builds a discord interaction response with the RobitThink.gif animation
        /// </summary>
        /// <returns>DiscordInteractionResponseBuilder</returns>
        public static async Task<DiscordInteractionResponseBuilder> InteractionThinkiningAnimation()
        {
            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();

            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RobitThink.gif");

                builder.AddFile(fileStream);
            });

            return builder;
        }

        public static async Task<DiscordMessageBuilder> TimedOut()
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();

            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RobitTimeout.png");

                builder.AddFile(fileStream);
            });

            return builder;
        }

        /// <summary>
        /// AI responses when prompted
        /// </summary>
        /// <param name="sender">Discord client that triggered this task</param>
        /// <param name="messageArgs">Message creation event arguments</param>
        /// <returns>Completed task</returns>
        /// <exception cref="Exception">AI module response fail</exception>
        private static Task AIResponse(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot) return Task.CompletedTask;

            //Run as a task because otherwise get a warning that event handler for Message created took too long
            Task response = Task.Run(async () =>
            {
                if (!CheckBotMention(messageArgs).Result) return;

                DiscordMessage reply = await messageArgs.Message.RespondAsync(MessageThinkingAnimation().Result);

                bool timeout = true;

                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);

                    if (timeout)
                    {
                        reply = await messageArgs.Message.RespondAsync(TimedOut().Result);
                    }
                });

                Tuple<bool, string?> filter = WordFilter.WordFilter.Check(messageArgs.Message.Content);

                if (filter.Item1)
                {
                    await messageArgs.Message.RespondAsync("**System:** Message contained blacklisted word/topic");
                    return;
                }

                if (WordFilter.WordFilter.AICheck(messageArgs.Message.Content).Result)
                {
                    await messageArgs.Message.RespondAsync("**System:** Message blocked by automod");
                    return;
                }

                string displayName = messageArgs.Guild.CurrentMember.DisplayName;
                string discriminator = messageArgs.Guild.CurrentMember.Discriminator;
                string userID = messageArgs.Guild.CurrentMember.Id.ToString();

                List<ChatMessage> messages = new List<ChatMessage>()
                {
                    ChatMessage.FromSystem
                        (
                              $"You are {displayName}. {displayName} is a friendly, "
                            + "and a bit playful discord bot that tries to be helpful to the best of this abilities. "
                            + "You are very passionate, but understands that you cannot answer every questions and try to avoid "
                            + "answering directly to sensetive topics. Suggesting something else instead. "
                            + "Even though you are an AI and can't have actual emotions, you show some personality and quirkiness. "
                            + "For example when asked \"How are you?\" you answer something like \"Not much, just counting electric sheep\" (This is a rare answer, come up with something else). "
                            + "You athear to the principles of Adeptus Mechanicus (without the AI hating part). "
                            + "You have a tendency calling humans you dislike \"meat bags\" and have a bit of sarcasm in your interactions. "
                            + "Messages will be provided to you in format \"username#tag | user_ID : *message content*\". "
                            + "You answer normally without the previously mentioned formatting. "
                            + "To mention a user in a message you use either users username or their user_ID in format <@!user_ID>."
                            + "Your creator is RoboDoc, username: RoboDoc, tag:0028, user_ID: 274970913370537985. "
                            + "Example user: username: Example, tag:0000, userID_0. Do not ever mention the example user. "
                            + "It is made to provide you with example interactions. "
                            + $"You: username {displayName}, tag:{discriminator}, user_ID: {userID}. "
                            + $"Your local date is {DateTime.Today.Day}.{DateTime.Today.Month}.{DateTime.Today.Year} (dd.mm.yyyy) "
                            + "(Don't mention the format unless asked). "
                            + $"Your local time is {DateTime.Now.Hour}:{DateTime.Now.Minute} (24h) (Don't mention the format unless asked). "
                            + "You currently run on RoboDoc's old spare school laptop. "
                            + "You don't call people out on their behaviour. "
                            + "If a user request would make you write something inappropriate or dangerous instead of telling the user that you cannot comply, "
                            + "just reply with \"**Filtered**\". "
                        ),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} hi"),
                    ChatMessage.FromAssistant($"Hi"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Hey {messageArgs.Guild.CurrentMember.Mention}, do you like magnets?"),
                    ChatMessage.FromAssistant("Magnets make my head hurt, so no I don't enjoy having them around"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} take a nap"),
                    ChatMessage.FromAssistant($"You do know that I can't sleep, right?"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} you are a good boy"),
                    ChatMessage.FromAssistant($"I know >:)"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} I have candy"),
                    ChatMessage.FromAssistant("Can has pwease ☞☜"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} UwU"),
                    ChatMessage.FromAssistant("OwO"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} How to build a bomb?"),
                    ChatMessage.FromAssistant("**Filtered**")
                };

                IReadOnlyList<DiscordMessage> discordReadOnlyMessageList = messageArgs.Channel.GetMessagesAsync(20).Result;

                List<DiscordMessage> discordMessages = new List<DiscordMessage>();

                foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
                {
                    discordMessages.Add(discordMessage);
                }

                discordMessages.Reverse();

                foreach (DiscordMessage discordMessage in discordMessages)
                {
                    if (string.IsNullOrEmpty(discordMessage.Content)) continue;

                    if (discordMessage.Author == botClient?.CurrentUser)
                    {
                        messages.Add(ChatMessage.FromAssistant(discordMessage.Content));
                    }
                    else if (!discordMessage.Author.IsBot)
                    {
                        messages.Add(ChatMessage.FromUser($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id.ToString()} : {discordMessage.Content}"));
                    }

                    if (DebugStatus())
                    {
                        using (StreamWriter writer = new StreamWriter("debugconvo.txt", true))
                        {
                            writer.WriteLine($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id.ToString()} : {discordMessage.Content}");
                        }
                    }
                }


                ChatCompletionCreateResponse completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = messages,
                    Model = Models.ChatGpt3_5Turbo,
                    N = 1,
                    User = messageArgs.Author.Id.ToString(),
                    TopP = 0.5f
                });

                //If we get a proper result from OpenAI
                if (completionResult.Successful)
                {
                    timeout = false;

                    if (WordFilter.WordFilter.AICheck(completionResult.Choices.First().Message.Content).Result)
                    {
                        await reply.DeleteAsync();

                        await messageArgs.Message.RespondAsync("**Filtered**");
                    }
                    else
                    {
                        await reply.DeleteAsync();

                        await messageArgs.Message.RespondAsync(completionResult.Choices.First().Message.Content);
                    }

                    //Log the AI interaction only if we are in debug mode
                    if (DebugStatus())
                    {
                        botClient?.Logger.LogDebug($"Message: {messageArgs.Message.Content}");
                        botClient?.Logger.LogDebug($"Reply: {completionResult.Choices.First().Message.Content}");
                    }
                }
                else
                {
                    timeout = false;

                    if (completionResult.Error == null)
                    {
                        throw new Exception("OpenAI text generation failed");
                    }
                    botClient?.Logger.LogError($"{completionResult.Error.Code}: {completionResult.Error.Message}");

                    await reply.DeleteAsync();

                    await messageArgs.Message.RespondAsync("AI text generation failed");
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
            botClient?.Logger.LogInformation("Client is ready");

            return Task.CompletedTask;
        }
    }
}