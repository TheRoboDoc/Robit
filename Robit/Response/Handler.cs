using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using static Robit.FileManager;

namespace Robit.Response
{
    public static class Handler
    {
        public static readonly EventId HandlerEvent = new EventId(301, "Handler");

        /// <summary>
        /// Runs the response handler that determines to respond or not and how
        /// </summary>
        /// <param name="sender">Discord client</param>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>Completed task</returns>
        public static async Task Run(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {                                                                     //Motherboard ID
            if (messageArgs.Author.IsBot && messageArgs?.Author.Id.ToString() != "1103797730276548660") return;

            if (await DiscordNoobFailsafe(messageArgs)) return;

            // Checking if we need to respond at all depending on channel settings
            ChannelManager.Channel channelSettings = ChannelManager.ReadChannelInfo(messageArgs.Guild.Id.ToString(), messageArgs.Channel.Id.ToString());

            bool responded = await AutoRespond(messageArgs, channelSettings);

            await AutoReact(sender, messageArgs);

            if (responded) { return; }

            if (channelSettings.AIIgnore) return;

            await AIRespond(messageArgs);
        }

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

        private static async Task AIRespond(MessageCreateEventArgs messageArgs)
        {
            DiscordChannel replyIn = messageArgs.Channel;

            // We want to reply if the message was sent in a thread that bot is a member of
            if (replyIn.Type == ChannelType.PublicThread || replyIn.Type == ChannelType.PrivateThread)
            {
                if (messageArgs.Author.IsBot)
                {
                    return;
                }

                DiscordThreadChannel threadChannel = (DiscordThreadChannel)replyIn;

                // This is stupid but for some magic reasons it didn't work otherwise
                IReadOnlyList<DiscordThreadChannelMember> romembers = await threadChannel.ListJoinedMembersAsync();

                List<DiscordThreadChannelMember> members = new List<DiscordThreadChannelMember>();

                foreach (DiscordThreadChannelMember member in romembers)
                {
                    members.Add(member);
                }

                // Suboptimal way to check if the bot is a memeber of the thread, but it works
                bool hasBot = false;

                foreach (DiscordThreadChannelMember member in members)
                {
                    if (member.Id == Program.BotClient?.CurrentUser.Id)
                    {
                        hasBot = true;
                        break;
                    }
                }

                if (!hasBot)
                {
                    return;
                }
            }
            else if (!await CheckBotMention(messageArgs))
            {
                return;
            }

            if (replyIn.Type != ChannelType.PublicThread)
            {
                // We are checking if within 9 messages there were 3 occurances of user message and same for bot message, if so we create a new
                // thread and reply in there.
                IReadOnlyList<DiscordMessage> discordReadOnlyMessageList = messageArgs.Channel.GetMessagesAsync(9).Result;

                List<DiscordMessage> discordMessagesFromUser = new List<DiscordMessage>();

                foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
                {
                    if (discordMessage.Author == messageArgs.Author)
                    {
                        discordMessagesFromUser.Add(discordMessage);
                    }
                }

                List<DiscordMessage> discordMessagesFromBot = new List<DiscordMessage>();

                foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
                {
                    if (discordMessage.Author == messageArgs.Guild.CurrentMember)
                    {
                        discordMessagesFromBot.Add(discordMessage);
                    }
                }

                if (discordMessagesFromUser.Count > 3 && discordMessagesFromBot.Count > 3)
                {
                    // Remove the mention string
                    string name = Regex.Replace(messageArgs.Message.Content, "<@!?(\\d+)>", "");

                    if (Program.DebugStatus())
                    {
                        Program.BotClient?.Logger.LogDebug(HandlerEvent, "Thread trigger");
                    }

                    // Create the thread
                    DiscordThreadChannel thread = await messageArgs.Channel.CreateThreadAsync(messageArgs.Message, name, AutoArchiveDuration.Day,
                                $"{messageArgs.Author.Mention} interacted multiple times in a row with the bot");

                    replyIn = thread;
                }
            }

            _ = Task.Run(async () =>
            {
                Task typing = replyIn.TriggerTypingAsync();

                Tuple<bool, string> AIGenerationResponse = await AI.GenerateChatResponse(messageArgs);

                string response = AIGenerationResponse.Item2;

                if (AIGenerationResponse.Item1)
                {
                    await replyIn.SendMessageAsync(response);
                }
                else
                {
                    await replyIn.SendMessageAsync("**System:** " + response);
                }
            });
        }

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
        /// A failsafe for when a user tries to execute a slash command but sends it as a plain message instead.
        /// Deletes the failed command message and after 10 seconds deletes the warning message.
        /// </summary>
        /// <param name="sender">Discord client that triggerd this task</param>
        /// <param name="messageArgs">Message creation event arguemnts</param>
        /// <returns>
        /// <list type="table">
        /// <item>True: Failsafe triggered</item>
        /// <item>False: Failsafe not triggered</item>
        /// </list>
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
            SlashCommandsExtension slashCommandsExtension = Program.BotClient.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension.RegisteredCommands;
            List<DiscordApplicationCommand> globalCommands =
                slashCommandsList.Where(x => x.Key == null).SelectMany(x => x.Value).ToList(); //This is stupid, can't find a better way as of yet

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

            return triggered;
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
                    if (mentionedUser == Program.BotClient?.CurrentUser)
                    {
                        botMentioned = true;
                        break;
                    }
                }
            });

            return botMentioned;
        }
    }
}
