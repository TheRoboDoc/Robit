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
        /// <summary>
        /// Runs the response handler that determines to respond or not and how
        /// </summary>
        /// <param name="sender">Discord client</param>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>Completed task</returns>
        public static async Task Run(DiscordClient sender, MessageCreateEventArgs messageArgs)
        {
            if (messageArgs.Author.IsBot) return;

            if (await DiscordNoobFailsafe(messageArgs)) return;

            ChannelManager.Channel channelSettings = ChannelManager.ReadChannelInfo(messageArgs.Guild.Id.ToString(), messageArgs.Channel.Id.ToString());

            if (channelSettings.autoResponse)
            {
                Tuple<bool, string> autoResponseResult = await Auto.GenerateAutoResponse(messageArgs);

                if (autoResponseResult.Item1)
                {
                    await messageArgs.Message.RespondAsync(autoResponseResult.Item2);
                    return;
                }
            }

            if (channelSettings.AIIgnore) return;

            string pattern = @"https?:\/\/(?:www\.)?(?:discordapp|discord)\.com\/.*|https?:\/\/(?:www\.)?cdn\.discordapp\.com\/.*";

            if (Regex.IsMatch(messageArgs.Message.Content, pattern)) return;

            if (string.IsNullOrEmpty(messageArgs.Message.Content) && messageArgs.Message.Attachments.Count > 0) return;

            Random rand = new Random();

            if (rand.Next(1, 7) == 6)
            {
                //Allow pass
            }
            else if (await CheckBotMention(messageArgs) == false)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                DiscordMessage reply = await messageArgs.Message.RespondAsync(await MessageThinkingAnimation());

                bool done = false;

                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);

                    if (!done)
                    {
                        try
                        {
                            await reply.ModifyAsync(await TimedOut());
                        }
                        catch
                        {
                            if (Program.DebugStatus())
                            {
                                Program.botClient?.Logger.LogDebug("Reply message was deleted or null");
                            }
                        }
                    }
                });

                Tuple<bool, string> AIGenerationResponse = await AI.GenerateChatResponse(messageArgs);

                done = true;

                await reply.DeleteAsync();

                string response = AIGenerationResponse.Item2;

                if (AIGenerationResponse.Item1)
                {
                    await messageArgs.Channel.SendMessageAsync(response);
                }
                else
                {
                    await messageArgs.Channel.SendMessageAsync("**System:** " + response);
                }
            });
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
        private static async Task<bool> DiscordNoobFailsafe(MessageCreateEventArgs messageArgs)
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
                    Program.botClient?.Logger.LogInformation("The message was empty");
                }
            }

            SlashCommandsExtension slashCommandsExtension = Program.botClient.GetSlashCommands();

            var slashCommandsList = slashCommandsExtension.RegisteredCommands;
            List<DiscordApplicationCommand> globalCommands =
                slashCommandsList.Where(x => x.Key == null).SelectMany(x => x.Value).ToList();

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
                    if (mentionedUser == Program.botClient?.CurrentUser)
                    {
                        botMentioned = true;
                        break;
                    }
                }
            });

            return botMentioned;
        }

        /// <summary>
        /// Builds a discord message with the RobitThink.gif animation
        /// </summary>
        /// <returns>DiscordMessageBuilder</returns>
        private static async Task<DiscordMessageBuilder> MessageThinkingAnimation()
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
        private static async Task<DiscordInteractionResponseBuilder> InteractionThinkiningAnimation()
        {
            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();

            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RobitThink.gif");

                builder.AddFile(fileStream);
            });

            return builder;
        }

        /// <summary>
        /// Builds a discord Discord message with the RobitTimeout.png
        /// </summary>
        /// <returns>DiscordMessageBuilder</returns>
        private static async Task<DiscordMessageBuilder> TimedOut()
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder();

            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RobitTimeout.png");

                builder.AddFile(fileStream);
            });

            return builder;
        }
    }
}
