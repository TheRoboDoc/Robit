using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Robit.TextAdventure;
using static Robit.FileManager;

namespace Robit.Response
{
    public static class Handler
    {
        public static readonly EventId HandlerEvent = new EventId(301, "Handler");

        /// <summary>
        ///     Runs the response handler that determines to respond or not and how
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Discord client
        /// </param>
        /// 
        /// <param name="messageArgs">
        ///     Discord message creation arguments
        /// </param>
        /// 
        /// <returns>
        ///     Completed task
        /// </returns>
        public static async Task Run(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {                                                                     //Motherboard ID
            if (messageArgs.Author.IsBot && messageArgs?.Author.Id.ToString() != "1103797730276548660") return;

            if (await DiscordNoobFailsafe(messageArgs)) return;

            if (await TextBasedAdventure(messageArgs)) return;

            // Checking if we need to respond at all depending on channel settings
            ChannelManager.Channel channelSettings = ChannelManager.ReadChannelInfo(messageArgs.Guild.Id.ToString(), messageArgs.Channel.Id.ToString());
                                      //Hag Breeders Anonymous                      //Testing Server
            if (messageArgs.Guild.Id == 884936240321929277 || messageArgs.Guild.Id == 766478619513585675)
            {
                string trigger = "give sauce";

                Program.BotClient?.Logger.LogDebug("{message}" ,messageArgs.Message.Content);
                Program.BotClient?.Logger.LogDebug("{message}", messageArgs.Message.Content.Length.ToString());
                Program.BotClient?.Logger.LogDebug("{message}", ($"{Program.BotClient?.CurrentUser.Mention} {trigger}".Length + 5).ToString());

                if (messageArgs.Message.Content.Contains(trigger) &&
                    messageArgs.Message.Content.Length <= $"{Program.BotClient?.CurrentUser.Mention} {trigger}".Length + 5 &&
                    await CheckBotMention(messageArgs))
                {
                    Random rnd = new Random();

                    int number = rnd.Next(1, 60000);

                    await messageArgs.Message.RespondAsync(number.ToString());

                    return;
                }
            }

            if (messageArgs.Guild.Id == 884936240321929277 || messageArgs.Guild.Id == 766478619513585675)
            {
                string trigger = "give sauce";

                if (messageArgs.Message.Content.Contains(trigger) &&
                    messageArgs.Message.Content.Length <= $"{Program.BotClient?.CurrentUser.Mention} {trigger}".Length + 5 &&
                    await CheckBotMention(messageArgs))
                {
                    Random rnd = new Random();

                    int number = rnd.Next(1, 60000);

                    await messageArgs.Message.RespondAsync(number.ToString());

                    return;
                }
            }

            bool responded = await AutoRespond(messageArgs, channelSettings);

            await AutoReact(sender, messageArgs);

            if (responded) { return; }

            if (channelSettings.AIIgnore) return;

            await AIRespond(messageArgs);
        }

        /// <summary>
        ///     Deletes a game instance from main game manager container. Also deletes the game instance channel after five minutes
        /// </summary>
        /// 
        /// <param name="gameManager">
        ///     Game manager of the instance that needs to be deleted
        /// </param>
        private static async Task DeleteGame(GameManager gameManager)
        {
            await gameManager.Channel.SendMessageAsync("**System**: This channel will be deleted in 5 minutes");

            Program.GameManagerContainer?.RemoveManager(gameManager);

            Thread.Sleep(TimeSpan.FromMinutes(5));

            await gameManager.Channel.DeleteAsync("Text-Base Adventure game ended");
        }

        /// <summary>
        ///     Managment of text based adventure instance
        /// </summary>
        /// 
        /// <param name="messageArgs">
        ///     Message arguments
        /// </param>
        /// 
        /// <returns>
        ///     <list type="table">
        ///         <item>
        ///             True: Successfully played a turn of text-based adventure
        ///         </item>
        /// 
        ///         <item>
        ///             False: Failed to play a trun of text-based adventure
        ///         </item>
        ///     </list>
        /// </returns>
        private static async Task<bool> TextBasedAdventure(MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Channel.Type != ChannelType.PrivateThread) return false;

            DiscordThreadChannel? thread = messageArgs.Channel as DiscordThreadChannel;

            if (thread?.Id != messageArgs.Guild.Threads.Keys.Where(threadId => threadId == thread?.Id).FirstOrDefault()) return false;

            GameManager? gameManager = Program.GameManagerContainer?.GetManagerByThread(thread);

            if (gameManager == null) return false;

            await thread.TriggerTypingAsync();

            GameManager.TurnResult turnResult = await gameManager.Run();

            if (!turnResult.Success)
            {
                await thread.SendMessageAsync($"**System:** {turnResult.AIAnswer}");

                if (turnResult.AIAnswer == GameManager.MaxTurnReachedMessage)
                {
                    _ = DeleteGame(gameManager);
                }

                return false;
            }

            await thread.SendMessageAsync(turnResult.AIAnswer);

            return true;
        }

        /// <summary>
        ///     Reacts to message
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Discord client
        /// </param>
        /// 
        /// <param name="messageArgs">
        ///     Discord message creation arguments
        /// </param>
        private static async Task AutoReact(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {
            Tuple<bool, string> autoReactResult = await Auto.GenerateAutoReact(messageArgs);

            bool result = autoReactResult.Item1;
            string reactResult = autoReactResult.Item2;

            if (result)
            {
                if (!DiscordEmoji.TryFromName(sender, reactResult, true, out DiscordEmoji emoji))
                {
                    sender.Logger.LogWarning("Failed to fetch a reaction emoji");
                    return;
                }

                await messageArgs.Message.CreateReactionAsync(emoji);
            }
        }

        /// <summary>
        ///     Responses with AI answer to the message
        /// </summary>
        /// 
        /// <param name="messageArgs">
        ///     Discord message creation arguments
        /// </param>
        private static async Task AIRespond(MessageCreateEventArgs messageArgs)
        {
            DiscordChannel replyIn = messageArgs.Channel;

            if (!await CheckBotMention(messageArgs))
            {
                return;
            }

            bool typing = true;

            _ = Task.Run(async () =>
            {
                while (typing)
                {
                    await replyIn.TriggerTypingAsync();

                    await Task.Delay(3000);
                }
            });

            Tuple<bool, string> AIGenerationResponse = await AI.GenerateChatResponse(messageArgs);

            typing = false;

            string response = AIGenerationResponse.Item2;

            if (AIGenerationResponse.Item1)
            {
                await replyIn.SendMessageAsync(response);
            }
            else
            {
                await replyIn.SendMessageAsync("**System:** " + response);
            }
        }

        /// <summary>
        ///     Auto responses to a message
        /// </summary>
        /// 
        /// <param name="messageArgs">
        ///     Discord message creation arguments
        /// </param>
        /// 
        /// <param name="channelSettings">
        ///     Channel settings
        /// </param>
        /// 
        /// <returns>
        ///     <list type="table">
        ///         <listheader>
        ///             Boolean
        ///         </listheader>
        /// 
        ///         <item>
        ///             True: Auto-response happened
        ///         </item>
        /// 
        ///         <item>
        ///             False: Auto-response didn't happen
        ///         </item>
        ///     </list>
        /// </returns>
        private static async Task<bool> AutoRespond(MessageCreateEventArgs messageArgs, ChannelManager.Channel channelSettings)
        {
            if (channelSettings.autoResponse)
            {
                Tuple<bool, string> autoResponseResult = await Auto.GenerateAutoResponse(messageArgs);

                if (autoResponseResult.Item1)
                {
                    await messageArgs.Message.RespondAsync(autoResponseResult.Item2);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     A failsafe for when a user tries to execute a slash command but sends it as a plain message instead.
        ///     Deletes the failed command message and after 10 seconds deletes the warning message.
        /// </summary>
        /// 
        /// <param name="sender">
        ///     Discord client that triggerd this task
        /// </param>
        /// 
        /// <param name="messageArgs">
        ///     Message creation event arguemnts
        /// </param>
        /// 
        /// <returns>
        ///     <list type="table">
        ///         <item>
        ///             True: Failsafe triggered
        ///         </item>
        ///         
        ///         <item>
        ///             False: Failsafe not triggered
        ///         </item>
        ///     </list>
        /// </returns>
        private static async Task<bool> DiscordNoobFailsafe(MessageCreateEventArgs messageArgs) //This is redundant as you need to fuck up pretty bad
        {
            if (messageArgs.Author.IsBot || messageArgs.Equals(null)) return false;

            try
            {
                if (messageArgs.Message.Content.First() != '/') return false;
            }
            catch
            {
                if (Program.DebugStatus())
                {
                    Program.BotClient?.Logger.LogInformation(HandlerEvent, "The message was empty");
                }
            }

            //Fetching every slash command the bot has
            SlashCommandsExtension? slashCommandsExtension = Program.BotClient?.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension?.RegisteredCommands;
            List<DiscordApplicationCommand>? globalCommands =
                slashCommandsList?.Where(x => x.Key == null).SelectMany(x => x.Value).ToList(); //This is stupid, can't find a better way as of yet

            if (globalCommands == null)
            {
                Program.BotClient?.Logger.LogWarning(HandlerEvent, "Failed to fetch commands");

                return false;
            }

            List<string> commands = new List<string>();

            foreach (DiscordApplicationCommand globalCommand in globalCommands)
            {
                commands.Add(globalCommand.Name);
            }

            DiscordMessage? message = null;

            bool triggered = false;

            foreach (string command in commands)
            {
                if (messageArgs.Message.Content.Contains(command))
                {
                    await messageArgs.Message.DeleteAsync();

                    message = await messageArgs.Message.RespondAsync
                        ($"{messageArgs.Author.Mention} you tried running a {command} command, but instead send it as a plain message. " +
                        $"That doesn't look very nice for you. So I took the liberty to delete it");

                    triggered = true;

                    // Deletes message
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);

                        try
                        {
                            await message.DeleteAsync();
                        }
                        catch
                        {
                            Program.BotClient?.Logger.LogWarning(HandlerEvent, "Couldn't delete message {messageID}", message?.Id);
                        }
                    });

                    break;
                }
            }

            return triggered;
        }

        /// <summary>
        ///     Checks if the bot was mentioned in a message
        /// </summary>
        /// 
        /// <param name="messageArgs">
        ///     Arguments of the message to check
        /// </param>
        /// 
        /// <returns>
        ///     <list type="bullet">
        ///         <item>
        ///             <c>True</c>: Mentioned
        ///         </item>
        ///         <item>
        ///             <c>False</c>: Not mentioned
        ///         </item>
        ///     </list>
        /// </returns>
        private static async Task<bool> CheckBotMention(MessageCreateEventArgs messageArgs)
        {
            bool botMentioned = false;

            await Task.Run(() =>
            {
                foreach (DiscordUser? mentionedUser in messageArgs.MentionedUsers)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    if (mentionedUser == Program.BotClient?.CurrentUser)
                    {
                        botMentioned = true;
                        break;
                    }
#pragma warning restore CS8604 // Possible null reference argument.
                }
            });

            return botMentioned;
        }
    }
}
