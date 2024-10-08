﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Robit.Response;
using Robit.TextAdventure;
using System.ComponentModel;
using System.Text.RegularExpressions;
using static Robit.FileManager;
using static Robit.FileManager.QuoteManager;

namespace Robit.Command
{
    internal class SlashCommands : ApplicationCommandModule
    {
        #region Technical
        [SlashCommand("Ping", "Pings the bot, the bot responds with the ping time in milliseconds")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public static async Task Ping(InteractionContext ctx,

        [Option("Times", "Amount of times the bot should be pinged (Max 3)")]
        [DefaultValue(1)]
        [Minimum(1)]
        [Maximum(3)]
        double times = 1,

        [Option("Visible", "Is the ping visible to others")]
        [DefaultValue(false)]
        bool visible = false)
        {
            await ctx.CreateResponseAsync($"Pong {ctx.Client.Ping}ms", !visible);
            times--;

            for (int i = 0; times > i; times--)
            {
                DiscordFollowupMessageBuilder followUp = new DiscordFollowupMessageBuilder()
                {
                    Content = $"Pong {ctx.Client.Ping}ms",
                    IsEphemeral = !visible
                };

                await ctx.FollowUpAsync(followUp);
            }
        }
        #endregion

        #region Help
        [SlashCommand("Commands", "Lists all commands for the bot")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public static async Task Commands(InteractionContext ctx,

        [Option("Visible", "Is the ping visible to others")]
        [DefaultValue(false)]
        bool visible = false)
        {
            SlashCommandsExtension? slashCommandsExtension = Program.BotClient?.GetSlashCommands();

            List<KeyValuePair<ulong?, IReadOnlyList<DiscordApplicationCommand>>>? slashCommandKeyValuePairs = slashCommandsExtension?.RegisteredCommands.ToList();

            IReadOnlyList<DiscordApplicationCommand>? slashCommands = slashCommandKeyValuePairs?.FirstOrDefault().Value;

            List<Page> pages = new List<Page>();

            int entriesPerPage = 5;
            int pageIndex = 0;

            if (slashCommands == null)
            {
                Program.BotClient?.Logger.LogWarning("Failed to fetch list of commands");

                await ctx.CreateResponseAsync("Failed to fetech list of commands");

                return;
            }

            while (pageIndex < slashCommands.Count)
            {
                List<DiscordApplicationCommand> pageEntries = slashCommands
                    .Skip(pageIndex)
                    .Take(entriesPerPage)
                    .ToList();

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                {
                    Title = "List of commands",
                    Color = DiscordColor.Purple,
                };

                foreach (DiscordApplicationCommand slashCommand in pageEntries)
                {
                    string nameRaw = slashCommand.Name;
                    string descriptionRaw = slashCommand.Description;

                    string name = char.ToUpper(nameRaw[0]) + nameRaw.Substring(1);
                    string description = char.ToUpper(descriptionRaw[0]) + descriptionRaw.Substring(1);

                    embed.AddField(name, description);

                    embed.WithFooter($"{pageIndex / entriesPerPage + 1}/{(slashCommands.Count + entriesPerPage - 1) / entriesPerPage}");
                }

                pages.Add(new Page { Embed = embed });

                pageIndex += entriesPerPage;
            }

            InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

            await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);
        }
        #endregion

        #region Interaction
        #region Introduction
        [SlashCommand("Intro", "Bot introduction")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public static async Task Intro(InteractionContext ctx,

        [Option("Visible", "Is the ping visible to others")]
        [DefaultValue(true)]
        bool visible = true)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Purple,

                Description =
                $"Hi I'm {ctx.Client.CurrentUser.Mention}. Your friendly neighborhood machine. My set of abilities is currently expanding. " +
                $"You can just chat with me in any channel that I have access to (I will ignore your DMs filthy human). " +
                $"I can write code for you, or post a funny gif if you want. " +
                $"Perhaps you want to see what commands I have? You can use the \"commands\" command and see for yourself dummy. " +
                $"Oh and well... if you don't know how to use slash commands... that's just too bad!",

                Timestamp = DateTimeOffset.Now,

                Title = "Hi!",
            }.AddField("GitHub", "Want to see what makes me tick or report a bug? Check my GitHub repo: \nhttps://github.com/TheRoboDoc/Robit ");

            await ctx.CreateResponseAsync(embed, !visible);
        }

        [SlashCommand("Github", "Posts a link to Robit's GitHub repo")]
        public static async Task GitHub(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("https://github.com/TheRoboDoc/Robit", true);
        }
        #endregion

        #region Automatic Reacts
        [SlashCommandGroup(
            "React",
            "Add, remove, or edit bot's reacts to messages")]
        [SlashCommandPermissions(Permissions.ManageEmojis | Permissions.AddReactions | Permissions.SendMessages)]
        public class React
        {
            private static bool EmoteValidity(DiscordClient client, string emoteString, out string filteredString)
            {
                string pattern = @"<(:.*?:)\d+>";

                Match match = Regex.Match(emoteString, pattern);

                string filteredEmote = emoteString;

                if (match.Success)
                {
                    filteredEmote = match.Groups[1].Value;
                }

                if (DiscordEmoji.TryFromUnicode(client, filteredEmote, out DiscordEmoji emoji))
                {
                    filteredString = emoji.GetDiscordName();

                    return true;
                }

                if (DiscordEmoji.TryFromName(client, filteredEmote, true, out _))
                {
                    filteredString = filteredEmote;

                    return true;
                }

                filteredString = string.Empty;

                return false;
            }

            [SlashCommand("Add", "Add a react interaction")]
            public static async Task Add(InteractionContext ctx,

            [Option("Name", "Name of the react interaction")]
            [MaximumLength(50)]
            string name,

            [Option("Trigger", "The trigger of the message to respond to")]
            [MaximumLength(50)]
            string trigger,

            [Option("React", "The reaction to the message")]
            string emote,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                if (!EmoteValidity(ctx.Client, emote, out string filteredEmote))
                {
                    await ctx.CreateResponseAsync("Failed to add the emote", true);
                    return;
                }

                EmoteReactManager.EmoteReactEntry responseEntry = new EmoteReactManager.EmoteReactEntry()
                {
                    ReactName = name,
                    Trigger = trigger,
                    DiscordEmoji = filteredEmote
                };

                List<EmoteReactManager.EmoteReactEntry>? allResponseEntries;

                allResponseEntries = EmoteReactManager.ReadEntries(ctx.Guild.Id.ToString());

                if (allResponseEntries != null)
                {
                    if (!allResponseEntries.Any())
                    {
                        foreach (EmoteReactManager.EmoteReactEntry entry in allResponseEntries)
                        {
                            if (entry.ReactName.ToLower() == responseEntry.ReactName.ToLower())
                            {
                                await ctx.CreateResponseAsync("A react with a same name already exists", true);
                                return;
                            }
                        }
                    }
                }

                EmoteReactManager.WriteEntry(responseEntry, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync($"Added new react entry with trigger '{trigger}'" +
                                              $" and react '{DiscordEmoji.FromName(ctx.Client, filteredEmote, true)}'", !visible);
            }

            [SlashCommand("Remove", "Remove a react interaction by a given name")]
            public static async Task Remove(InteractionContext ctx,

            [Option("Name", "Name of the react interaction to delete")]
            [MaximumLength(50)]
            string name,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visibile = false)
            {
                if (await EmoteReactManager.RemoveEntry(name, ctx.Guild.Id.ToString()))
                {
                    await ctx.CreateResponseAsync($@"Entry with a name {name} has been removed", !visibile);
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find a react entry with a name {name}", true);
                }
            }

            [SlashCommand("Modify", "Modify a react entry")]
            public static async Task Modify(InteractionContext ctx,

            [Option("Name", "Name of the react interaction to modify")]
            string name,

            [Option("Trigger", "New trigger for the interaction")]
            [MaximumLength(50)]
            string content,

            [Option("Emote", "New emote for the interaction")]
            [MaximumLength(50)]
            string emote,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                if (!EmoteValidity(ctx.Client, emote, out string filteredString))
                {
                    await ctx.CreateResponseAsync("Invalid emote!", true);
                    return;
                }

                if (EmoteReactManager.ModifyEntry(name, content, filteredString, ctx.Guild.Id.ToString()).Result)
                {
                    await ctx.CreateResponseAsync
                        (
                            $@"React entry with a name '{name}' has been modified, " +
                            $@"it now reacts to messages that contain '{content}' " +
                            $@"with '{DiscordEmoji.FromName(ctx.Client, filteredString, true)}'", !visible
                        );
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find react interaction with a name {name}", true);
                }
            }

            [SlashCommand("List", "List all the react interactions")]
            public static async Task List(InteractionContext ctx,

            [Option("Visible", "Sets the commands visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                List<EmoteReactManager.EmoteReactEntry>? reactEntires = new List<EmoteReactManager.EmoteReactEntry>();

                await Task.Run(async () =>
                {
                    reactEntires = EmoteReactManager.ReadEntries(ctx.Guild.Id.ToString());

                    if (reactEntires == null)
                    {
                        await ctx.CreateResponseAsync("Server has no react entries");
                        return;
                    }
                    else if (!reactEntires.Any())
                    {
                        await ctx.CreateResponseAsync("Server has no react entries");
                        return;
                    }

                    List<Page> pages = new List<Page>();

                    int entriesPerPage = 5;
                    int pageIndex = 0;

                    while (pageIndex < reactEntires.Count)
                    {
                        List<EmoteReactManager.EmoteReactEntry> pageEntries = reactEntires
                            .Skip(pageIndex)
                            .Take(entriesPerPage)
                            .ToList();

                        DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder
                        {
                            Title = "List of all responses",
                            Color = DiscordColor.Purple
                        };

                        foreach (EmoteReactManager.EmoteReactEntry reactEntry in pageEntries)
                        {
                            discordEmbedBuilder.AddField
                            (
                                reactEntry.ReactName,
                                $"{reactEntry.Trigger}: {DiscordEmoji.FromName(ctx.Client, reactEntry.DiscordEmoji, true)}"
                            );

                            discordEmbedBuilder.WithFooter($"{pageIndex / entriesPerPage + 1}/{(reactEntires.Count + entriesPerPage - 1) / entriesPerPage}"); //Page number
                        }

                        pages.Add(new Page { Embed = discordEmbedBuilder });

                        pageIndex += entriesPerPage;
                    }

                    InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                    await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);
                });
            }

            [SlashCommand("Wipe", "Wipe all of the react interactions")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public static async Task Wipe(InteractionContext ctx,

            [Option("Are_You_Sure", "Aure you sure you want to wipe all react on the server?")]
            bool check,

            [Option("visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                if (!check)
                {
                    await ctx.CreateResponseAsync("`Are You Sure` was set to `false` canceling...");

                    return;
                }

                if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator)) //Double checking, just in case
                {
                    await ctx.CreateResponseAsync("You don't have admin permission to execute this command");

                    return;
                }

                List<EmoteReactManager.EmoteReactEntry> responseEntries = new List<EmoteReactManager.EmoteReactEntry>();

                EmoteReactManager.OverwriteEntries(responseEntries, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync("All react interactions have been overwritten", !visible);
            }


        }
        #endregion

        #region Automatic Responses
        [SlashCommandGroup(
        "Response",
        "Add, remove, or edit bot's responses to messages.")]
        [SlashCommandPermissions(Permissions.ManageEmojis | Permissions.AddReactions | Permissions.SendMessages)]
        public class Response
        {
            [SlashCommand("Add", "Add a response interaction")]
            public static async Task Add(InteractionContext ctx,

            [Option("Name", "Name of the response interaction")]
            [MaximumLength(50)]
            string name,

            [Option("Trigger", "The trigger of the message to respond to")]
            [MaximumLength(50)]
            string content,

            [Option("Response", "The response to the message")]
            [MaximumLength(150)]
            string response,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                ResponseManager.ResponseEntry responseEntry = new ResponseManager.ResponseEntry()
                {
                    reactName = name,
                    content = content,
                    response = response
                };

                List<ResponseManager.ResponseEntry>? allResponseEntries;
                allResponseEntries = ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

                if (allResponseEntries != null)
                {
                    if (!allResponseEntries.Any())
                    {
                        foreach (ResponseManager.ResponseEntry entry in allResponseEntries)
                        {
                            if (entry.reactName.ToLower() == responseEntry.reactName.ToLower())
                            {
                                await ctx.CreateResponseAsync("A response with a same name already exists", true);
                                return;
                            }
                        }
                    }
                }

                ResponseManager.WriteEntry(responseEntry, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync($@"Added new response with trigger '{content}' and response '{response}'", !visible);
            }


            [SlashCommand("Remove", "Remove a response interaction by a given name")]
            public static async Task Remove(InteractionContext ctx,

            [Option("Name", "Name of the response interaction to delete")]
            [MaximumLength(50)]
            string name,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visibile = false)
            {
                if (ResponseManager.RemoveEntry(name, ctx.Guild.Id.ToString()))
                {
                    await ctx.CreateResponseAsync($@"Entry with a name {name} has been removed", !visibile);
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find a response with a name {name}", true);
                }
            }


            [SlashCommand("Modify", "Modify a response")]
            public static async Task Modify(InteractionContext ctx,

            [Option("Name", "Name of the response interaction to modify")]
            string name,

            [Option("Trigger", "New trigger for the interaction")]
            [MaximumLength(50)]
            string content,

            [Option("Response", "New response for the interaction")]
            [MaximumLength(150)]
            string response,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                if (ResponseManager.ModifyEntry(name, content, response, ctx.Guild.Id.ToString()).Result)
                {
                    await ctx.CreateResponseAsync
                        (
                            $@"Entry with a name '{name}' has been modified, " +
                            $@"it now responds to messages that contain '{content}' " +
                            $@"with '{response}'", !visible
                        );
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find response interaction with a name {name}", true);
                }
            }


            [SlashCommand("List", "List all the response interactions")]
            public static async Task List(InteractionContext ctx,

            [Option("Visible", "Sets the commands visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                List<ResponseManager.ResponseEntry>? responseEntries = new List<ResponseManager.ResponseEntry>();

                await Task.Run(async () =>
                {
                    responseEntries = ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

                    if (responseEntries == null)
                    {
                        await ctx.CreateResponseAsync("Server has no response entries");
                        return;
                    }
                    else if (!responseEntries.Any())
                    {
                        await ctx.CreateResponseAsync("Server has no response entries");
                        return;
                    }

                    List<Page> pages = new List<Page>();

                    int entriesPerPage = 5;
                    int pageIndex = 0;

                    while (pageIndex < responseEntries.Count)
                    {
                        List<ResponseManager.ResponseEntry> pageEntries = responseEntries
                            .Skip(pageIndex)
                            .Take(entriesPerPage)
                            .ToList();

                        DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder
                        {
                            Title = "List of all responses",
                            Color = DiscordColor.Purple
                        };

                        foreach (ResponseManager.ResponseEntry responseEntry in pageEntries)
                        {
                            discordEmbedBuilder.AddField
                            (
                                responseEntry.reactName,
                                $"{responseEntry.content}: {responseEntry.response}"
                            );

                            discordEmbedBuilder.WithFooter($"{pageIndex / entriesPerPage + 1}/{(responseEntries.Count + entriesPerPage - 1) / entriesPerPage}"); //Page number
                        }

                        pages.Add(new Page { Embed = discordEmbedBuilder });

                        pageIndex += entriesPerPage;
                    }

                    InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                    await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);
                });
            }

            [SlashCommand("Wipe", "Wipe all of the response interactions")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public static async Task Wipe(InteractionContext ctx,

            [Option("Are_You_Sure", "Aure you sure you want to wipe all responses on the server?")]
            bool check,

            [Option("visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                if (!check)
                {
                    await ctx.CreateResponseAsync("`Are You Sure` was set to `false` canceling...");

                    return;
                }

                if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator)) //Double checking, just in case
                {
                    await ctx.CreateResponseAsync("You don't have admin permission to execute this command");

                    return;
                }

                List<ResponseManager.ResponseEntry> responseEntries = new List<ResponseManager.ResponseEntry>();

                ResponseManager.OverwriteEntries(responseEntries, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync("All response interactions have been overwritten", !visible);
            }

            [SlashCommand("Ignore", "Should Robit's auto response ignore this channel or not")]
            [SlashCommandPermissions(Permissions.ManageChannels | Permissions.ManageMessages)]
            public static async Task Ignore(InteractionContext ctx,

            [Option("Ignore", "To ignore or not, true will ignore, false will not")]
            bool ignore,

            [Option("Visible", "Sets the visibility")]
            [DefaultValue(true)]
            bool visible = true)
            {
                string guildID = ctx.Guild.Id.ToString();
                string channelID = ctx.Channel.Id.ToString();

                ChannelManager.Channel channel = ChannelManager.ReadChannelInfo(guildID, channelID);

                channel.autoResponse = !ignore;

                ChannelManager.WriteChannelInfo(channel, guildID, channelID, true);

                await ctx.CreateResponseAsync($"Ignore this channel: `{ignore}`", !visible);
            }
        }
        #endregion

        #region Media Convert
        /// <summary>
        /// List of available file formats
        /// </summary>
        public enum FileFormats
        {
            mp4,
            mov,
            mkv,
            webm,
            gif,
            jpg,
            png,
            tiff
        }

        [SlashCommand("Convert", "Converts a given file from one format to another")]
        [SlashCommandPermissions(Permissions.AttachFiles | Permissions.SendMessages)]
        public static async Task Convert(InteractionContext ctx,
        [Option("Media_file", "Media file to convert from")] DiscordAttachment attachment,
        [Option("Format", "Format to convert to")] FileFormats fileFormat,
        [Option("Visible", "Sets the visibility", true)][DefaultValue(false)] bool visible = false)
        {
            await MediaManager.ClearChannelTempFolder(ctx.Interaction.Id.ToString());

            string[] mediaType = attachment.MediaType.Split('/');

            string type = mediaType[0];
            string format = mediaType[1];

            if (type != "image" && type != "video")
            {
                await ctx.CreateResponseAsync($"The given file format is '{type}' and not an image or an video", true);
                return;
            }

            if (format == fileFormat.GetName())
            {
                await ctx.CreateResponseAsync($"You tried to convert an '{format}' into an '{fileFormat.GetName()}'", true);
                return;
            }

            if (type == "video")
            {
                switch (fileFormat.GetName())
                {
                    case "jpg":
                    case "png":
                    case "tiff":
                        await ctx.CreateResponseAsync($"You tried to convert a video into an image. " +
                            $"{ctx.Guild.CurrentMember.DisplayName} doesn't support turning video into image sequences", true);
                        return;
                }
            }
            else if (type == "image")
            {
                switch (fileFormat.GetName())
                {
                    case "mp4":
                    case "mov":
                    case "mkv":
                    case "webm":
                        await ctx.CreateResponseAsync($"You tried to convert an image into a video", true);
                        return;
                }
            }

            if (attachment.FileSize > 8388608)
            {
                await ctx.CreateResponseAsync($"Sorry but the file size was above 8 Mb ({attachment.FileSize / 1048576} Mb)", true);
                return;
            }

            await ctx.CreateResponseAsync("https://cdn.discordapp.com/attachments/1051011721755623495/1085873228049809448/RobitThink.gif", !visible);

            bool timeout = true;

            _ = Task.Run(async () =>
            {
                await Task.Delay(60000);

                if (timeout)
                {
                    DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                    builder.WithContent("https://cdn.discordapp.com/attachments/1051011721755623495/1086753687008976926/RobitTimeout.png");

                    await ctx.EditResponseAsync(builder);
                }
            });

            //Saving
            MediaManager.SaveFile(attachment.Url, ctx.Interaction.Id.ToString(), format).Wait();

            //Converting
            await MediaManager.Convert(ctx.Interaction.Id.ToString(), format, fileFormat.GetName());

            string path = $"{MediaManager.IDToPath(ctx.Interaction.Id.ToString())}/output.{fileFormat.GetName()}";

            FileInfo fileInfo = new FileInfo(path);

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

            if (fileInfo.Length > 8388608)
            {
                timeout = false;

                builder.WithContent($"Sorry but the resulting file was above 8 Mb");

                await ctx.EditResponseAsync(builder);
                return;
            }

            FileStream fileStream = File.OpenRead(path);

            string responseText = $"{ctx.Member.Mention} {attachment.FileName} has been converted into {fileFormat.GetName()}";

            DiscordWebhookBuilder builder2 = new DiscordWebhookBuilder();

            //Sending
            builder2.AddMention(UserMention.All);
            builder2.WithContent(responseText);
            builder2.AddFile(fileStream);

            await ctx.EditResponseAsync(builder2);

            fileStream.Close();

            timeout = false;

            await MediaManager.ClearChannelTempFolder(ctx.Interaction.Id.ToString());
        }
        #endregion

        #region Tag Voice
        [SlashCommand("Tagvoice", "Tags everyone in the same voice channel as you")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public static async Task TagVoice(InteractionContext ctx,

        [Option("Message", "Message to ping people in current voice chat with")]
        [DefaultValue("")]
        string message = "",

        [Option("Attachment", "File attachment to the ping message")]
        [DefaultValue(null)]
        DiscordAttachment? attachment = null)
        {
            if (ctx.Member.VoiceState.Channel.Id != ctx.Guild.Channels.Keys.Where(channelId => channelId == ctx.Member.VoiceState.Channel.Id).FirstOrDefault())
            {
                await ctx.CreateResponseAsync("You must be in a voice chat", true);
                return;
            }

            DiscordChannel voiceChannel = ctx.Member.VoiceState.Channel;

            if (string.IsNullOrEmpty(message) && attachment == null)
            {
                await ctx.CreateResponseAsync("Message and attachment cannot both be empty", true);
                return;
            }

            string content = "";

            foreach (DiscordMember user in voiceChannel.Users)
            {
                if (!user.IsBot && user != ctx.Member)
                {
                    content += $"{user.Mention} ";
                }
            }

            content += $"\n{$"{ctx.Member.Mention} wanted people in {voiceChannel.Mention} to see this:\n"}";

            if (message != "")
            {
                content += $"\n{message}";
            }

            if (attachment != null)
            {
                content += $"\n{attachment.Url}";
            }

            DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder();

            responseBuilder.AddMention(UserMention.All);
            responseBuilder.WithContent(content);

            await ctx.CreateResponseAsync(responseBuilder);
        }
        #endregion

        #region AI Interactions
        [SlashCommand("Prompt", "Prompt the bot for a text response")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public static async Task Text(InteractionContext ctx,

        [Option("AI_prompt", "The AI text prompt")]
        [MaximumLength(690)]
        string prompt,

        [Option("Visible", "Sets the visibility", true)]
        [DefaultValue(true)]
        bool visible = true)
        {
            await ctx.CreateResponseAsync("https://cdn.discordapp.com/attachments/1051011721755623495/1085873228049809448/RobitThink.gif", !visible);

            bool timeout = true;

            _ = Task.Run(async () =>
            {
                await Task.Delay(20000);

                if (timeout)
                {
                    DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                    builder.WithContent("https://cdn.discordapp.com/attachments/1051011721755623495/1086753687008976926/RobitTimeout.png");

                    await ctx.EditResponseAsync(builder);
                }
            });

            Tuple<bool, string> AIResponse = await AI.GeneratePromptResponse(ctx, prompt);

            timeout = false;

            string response = AIResponse.Item2;

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

            if (AIResponse.Item1)
            {
                response = $"**Prompt:**\n{prompt}\n**Reply:**\n{response}";

                builder.WithContent(response);
            }
            else
            {
                response = $"**Prompt:**\n{prompt}\n**System:**\n{response}";

                builder.WithContent(response);
            }

            await ctx.EditResponseAsync(builder);
        }

        [SlashCommand("AI_Ignore", "Should Robit's AI module ignore this channel, prompt command will still work")]
        [SlashCommandPermissions(Permissions.ManageChannels | Permissions.ManageMessages)]
        public static async Task AIIgnore(InteractionContext ctx,

        [Option("Ignore", "To ignore or not, true will ignore, false will not")]
        bool ignore,

        [Option("Visible", "Sets the visibility")]
        [DefaultValue(true)]
        bool visible = true)
        {
            string guildID = ctx.Guild.Id.ToString();
            string channelID = ctx.Channel.Id.ToString();

            ChannelManager.Channel channel = ChannelManager.ReadChannelInfo(guildID, channelID);

            channel.AIIgnore = ignore;

            ChannelManager.WriteChannelInfo(channel, guildID, channelID, true);

            await ctx.CreateResponseAsync($"Ignore this channel: `{ignore}`", !visible);
        }

        #endregion

        #region Random generation
        [SlashCommandGroup("Random", "A set of commands to generate random values")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public class RandomCommands
        {
            public enum DiceTypes
            {
                D2 = 2,
                D4 = 4,
                D6 = 6,
                D8 = 8,
                D10 = 10,
                D12 = 12,
                D20 = 20,
            }

            [SlashCommand("Number", "Generates a psudorandom number within given range")]
            public static async Task Number(InteractionContext ctx,

            [Option("Maximum_value", "Maximum value for the random number")]
            [Minimum(0)]
            [Maximum(int.MaxValue)]
            double maximal,

            [Option("Minimal_value", "Minimal value for the random number")]
            [DefaultValue(0)]
            [Minimum(0)]
            [Maximum(int.MaxValue)]
            double minimal = 0,

            [Option("Visible", "Sets the visibility")]
            [DefaultValue(true)]
            bool visible = true)
            {
                Random rand = new Random();

                if (minimal >= maximal)
                {
                    await ctx.CreateResponseAsync("Minimal value cannot be larger or equal to maximal value", true);
                    return;
                }

                if (minimal < 0 || maximal < 0)
                {
                    await ctx.CreateResponseAsync("The minimal or maximal value cannot be a negative number", true);
                    return;
                }

                int randomValue = rand.Next((int)minimal, (int)maximal);

                await ctx.CreateResponseAsync($"Random number: {randomValue}", !visible);
            }

            [SlashCommand("Dice", "Roll dice")]
            public static async Task Dice(InteractionContext ctx,

            [Option("Dice_type", "Type of dice to roll")]
            DiceTypes dice,

            [Option("Amount", "Amount of dice to roll")]
            [Minimum(1)]
            [Maximum(255)]
            [DefaultValue(1)]
            double amount = 1,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(true)]
            bool visible = true)
            {
                Random rand = new Random();
                int maxValue = (int)dice + 1;

                List<int> rolledValues = new List<int>();

                for (int i = 0; i < amount; i++)
                {
                    rolledValues.Add(rand.Next(1, maxValue));
                }

                rolledValues.Sort();

                string diceResult = "";

                foreach (int rolledValue in rolledValues)
                {
                    diceResult += $"{rolledValue} ";
                }

                int sum = rolledValues.Sum();
                int average = sum / rolledValues.Count;
                int min = rolledValues.Min();
                int max = rolledValues.Max();
                float median;
                float mean = sum / rolledValues.Count;

                if (rolledValues.Count % 2 == 0) //even
                {
                    //(X[n / 2] + X[(n / 2) + 1]) / 2

                    float pos1 = rolledValues[rolledValues.Count / 2];
                    float pos2 = rolledValues[(rolledValues.Count / 2) + 1];

                    median = (pos1 + pos2) / 2;
                }
                else
                {
                    //X[(n + 1) / 2]

                    median = rolledValues[(rolledValues.Count + 1) / 2];
                }

                Dictionary<int, int> frequencyMap = new Dictionary<int, int>();

                // Count the frequency of each number
                foreach (int rolledValue in rolledValues)
                {
                    if (frequencyMap.TryGetValue(rolledValue, out int value))
                    {
                        frequencyMap[rolledValue] = ++value;
                    }
                    else
                    {
                        frequencyMap[rolledValue] = 1;
                    }
                }

                // Find the maximum frequency
                int maxFrequency = frequencyMap.Values.Max();

                // Find the numbers with the maximum frequency (modes)
                List<int> modes = frequencyMap.Where(pair => pair.Value == maxFrequency).Select(pair => pair.Key).ToList();

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Purple,

                    Title = $"Rolled {amount} {dice}s",

                    Timestamp = DateTime.Now
                };

                embed.AddField("Dice results", diceResult);

                if (amount > 1)
                {
                    embed.AddField("Sum", $"{sum}");
                    embed.AddField("Average", $"{average}");
                    embed.AddField("Median", $"{median}");
                    embed.AddField("Mean", $"{mean}");
                    embed.AddField("Mode(s)", string.Join(", ", modes));
                    embed.AddField("Min", $"{min}");
                    embed.AddField("Max", $"{max}");
                }

                await ctx.CreateResponseAsync(embed, !visible);
            }
        }
        #endregion

        #region Quotes
        [SlashCommandGroup("Wh40kquote", "A set of commands to post 40k quotes")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public class Wh40kQuotes
        {
            private static List<QuoteEntry>? quoteEntries;

            public enum Selection
            {
                First,
                At_Random
            }

            [SlashCommand("By_Author", "Search quotes by in universe author")]
            public static async Task ByAuthor(InteractionContext ctx,

            [Option("Search", "Search term to search by")]
            [MaximumLength(40)]
            string searchTerm,

            [Option("Result_Type", "Type of result you want to be fetch")]
            [DefaultValue(Selection.At_Random)]
            Selection selection = Selection.At_Random,

            [Option("Count", "Max amount of matches to return", true)]
            [Minimum(1)]
            [Maximum(10)]
            [DefaultValue(1)]
            double count = 1,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                quoteEntries ??= FetchAllEntries();

                List<QuoteEntry>? foundEntries;

                Random rand = new Random();

                if (selection == Selection.At_Random)
                {
                    foundEntries = FetchByAuthor(searchTerm, int.MaxValue, quoteEntries);
                }
                else
                {
                    foundEntries = FetchByAuthor(searchTerm, (int)count, quoteEntries);
                }

                if (foundEntries == null)
                {
                    await ctx.CreateResponseAsync("Failed to fetch Warhammer 40k quote", true);

                    return;
                }
                else if (!foundEntries.Any())
                {
                    await ctx.CreateResponseAsync("Didn't find any quotes by that author search", true);

                    return;
                }

                InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                List<Page> pages = new List<Page>();

                if (selection == Selection.At_Random)
                {
                    int max = foundEntries.Count;

                    for (int i = 0; i < (int)count; i++)
                    {
                        QuoteEntry entry = foundEntries.ElementAt(rand.Next(max));

                        string quoteText = $"***\"{entry.quote}\"***";

                        if (!string.IsNullOrEmpty(entry.author))
                        {
                            quoteText += $"\n*⎯ {entry.author}*";
                        }

                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.Purple,
                            Description = quoteText
                        };

                        if (!string.IsNullOrEmpty(entry.bookSource))
                        {
                            embedBuilder.AddField("Source:", entry.bookSource);
                        }

                        embedBuilder.WithFooter($"Quote search for in universe author using search term '{searchTerm}'" +
                            $"\n{i + 1}/{count}");

                        pages.Add(new Page { Embed = embedBuilder });
                    }

                    await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);

                    return;
                }

                foreach (QuoteEntry entry in foundEntries)
                {
                    string quoteText = $"***\"{entry.quote}\"***";

                    if (!string.IsNullOrEmpty(entry.author))
                    {
                        quoteText += $"\n*⎯ {entry.author}*";
                    }

                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Purple,
                        Description = quoteText
                    };

                    if (!string.IsNullOrEmpty(entry.bookSource))
                    {
                        embedBuilder.AddField("Source:", entry.bookSource);
                    }

                    embedBuilder.WithFooter($"Quote search for in universe author using search term '{searchTerm}'" +
                        $"\n{foundEntries.IndexOf(entry) + 1}/{foundEntries.Count}");

                    pages.Add(new Page { Embed = embedBuilder });
                }

                await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);
            }

            [SlashCommand("By_Source", "Search quotes by source")]
            public static async Task BySource(InteractionContext ctx,

            [Option("Search", "Search term to search by")]
            [MaximumLength(40)]
            string searchTerm,

            [Option("Result_Type", "Type of result you want to be fetch")]
            [DefaultValue(Selection.At_Random)]
            Selection selection = Selection.At_Random,

            [Option("Count", "Max amount of matches to return", true)]
            [Minimum(1)]
            [Maximum(10)]
            [DefaultValue(1)]
            double count = 1,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                quoteEntries ??= FetchAllEntries();

                List<QuoteEntry>? foundEntries;

                Random rand = new Random();

                if (selection == Selection.At_Random)
                {
                    foundEntries = FetchBySource(searchTerm, int.MaxValue, quoteEntries);
                }
                else
                {
                    foundEntries = FetchBySource(searchTerm, (int)count, quoteEntries);
                }

                if (foundEntries == null)
                {
                    await ctx.CreateResponseAsync("Failed to fetch Warhammer 40k quote", true);

                    return;
                }
                else if (!foundEntries.Any())
                {
                    await ctx.CreateResponseAsync("Didn't find any quotes by that author search", true);

                    return;
                }

                InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                List<Page> pages = new List<Page>();

                if (selection == Selection.At_Random)
                {
                    int max = foundEntries.Count;

                    for (int i = 0; i < (int)count; i++)
                    {
                        QuoteEntry entry = foundEntries.ElementAt(rand.Next(max));

                        string quoteText = $"***\"{entry.quote}\"***";

                        if (!string.IsNullOrEmpty(entry.author))
                        {
                            quoteText += $"\n*⎯ {entry.author}*";
                        }

                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.Purple,
                            Description = quoteText
                        };

                        if (!string.IsNullOrEmpty(entry.bookSource))
                        {
                            embedBuilder.AddField("Source:", entry.bookSource);
                        }

                        embedBuilder.WithFooter($"Quote search for real life source using search term '{searchTerm}'" +
                            $"\n{i + 1}/{count}");

                        pages.Add(new Page { Embed = embedBuilder });
                    }

                    await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);

                    return;
                }

                foreach (QuoteEntry entry in foundEntries)
                {
                    string quoteText = $"***\"{entry.quote}\"***";

                    if (!string.IsNullOrEmpty(entry.author))
                    {
                        quoteText += $"\n*⎯ {entry.author}*";
                    }

                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Purple,
                        Description = quoteText
                    };

                    if (!string.IsNullOrEmpty(entry.bookSource))
                    {
                        embedBuilder.AddField("Source:", entry.bookSource);
                    }

                    embedBuilder.WithFooter($"Quote search for real life source using search term '{searchTerm}'" +
                        $"\n{foundEntries.IndexOf(entry) + 1}/{foundEntries.Count}");

                    pages.Add(new Page { Embed = embedBuilder });
                }

                await interactivity.SendPaginatedResponseAsync(ctx.Interaction, !visible, ctx.Member, pages);
            }

            [SlashCommand("Random", "Fetches a random Warhammer 40k quote")]
            public static async Task RandomQuote(InteractionContext ctx,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(true)]
            bool visible = true)
            {
                quoteEntries ??= FetchAllEntries();

                Random rand = new Random();

                if (quoteEntries == null)
                {
                    await ctx.CreateResponseAsync("Failed to fetch Warhammer 40k quote", true);

                    return;
                }
                else if (!quoteEntries.Any())
                {
                    await ctx.CreateResponseAsync("Failed to fetch Warhammer 40k quote", true);

                    return;
                }

                QuoteEntry quoteEntry = quoteEntries.ElementAt(rand.Next(quoteEntries.Count));

                string quoteText = $"***\"{quoteEntry.quote}\"***";

                if (!string.IsNullOrEmpty(quoteEntry.author))
                {
                    quoteText += $"\n*⎯ {quoteEntry.author}*";
                }

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Purple,
                    Description = quoteText
                };

                if (!string.IsNullOrEmpty(quoteEntry.bookSource))
                {
                    embedBuilder.AddField("Source:", quoteEntry.bookSource);
                }

                await ctx.CreateResponseAsync(embedBuilder, !visible);
            }
        }
        #endregion

        #region Text Based Adventure Game
        [SlashCommandGroup("TBA", "Text Based Adventure")]
        [SlashCommandPermissions(Permissions.SendMessages | Permissions.SendMessagesInThreads | Permissions.CreatePrivateThreads)]
        public class TextBasedAdventureGame
        {
            [SlashCommand("Create", "Creates an instance of the game")]
            public static async Task Create(InteractionContext ctx,

            [Option("Game_Theme", "The theme of the adventure")]
            string gameTheme,

            [Option("Max_Turn_Count_Per_Player", "The maximum amount of turns allowed per player")]
            [DefaultValue(20)]
            [Minimum(0)]
            [Maximum(20)]
            double maxTurnCountPerPlayer = 20,

            [Option("Game_Name", "The name of the game")]
            [DefaultValue("")]
            string gameName = "",

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(true)]
            bool visible = true)
            {
                DiscordMember[] playerArray = { ctx.Member };

                Random rand = new Random();

                if (string.IsNullOrEmpty(gameName))
                {
                    gameName = rand.Next().ToString();
                }

                await ctx.CreateResponseAsync("Creating new game instance...", !visible);

                GameManager gameManager = await GameManager.Start(playerArray, gameName, gameTheme, ctx, (uint)maxTurnCountPerPlayer);

                await gameManager.Channel.TriggerTypingAsync();

                GameManager.TurnResult turnResult = await gameManager.Run();

                DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();

                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder()
                {
                    Title = "New text based adventure game instance",
                    Description = "Created a new instance of text based adventure game",
                    Color = DiscordColor.Purple
                };

                discordEmbedBuilder.AddField("Theme", $"{gameTheme}");

                string playersString = string.Empty;

                foreach (DiscordMember player in playerArray)
                {
                    playersString += $"{player.Mention}\n";
                }

                discordEmbedBuilder.AddField("Players", playersString);

                builder.AddEmbed(discordEmbedBuilder);

                builder.AddMentions(Mentions.All);

                await ctx.FollowUpAsync(builder);

                DiscordMember[] players = gameManager.Players;

                foreach (DiscordMember player in players)
                {
                    await gameManager.Channel.AddThreadMemberAsync(player);
                }

                await gameManager.Channel.SendMessageAsync(turnResult.AIAnswer);
            }
        }
        #endregion

        #endregion

        #region Autroles
        [SlashCommandGroup("Autorole", "Automatic on join roles")]
        [SlashCommandPermissions(Permissions.ManageRoles)]
        public class Autoroles
        {
            [SlashCommand("Add", "Add automatic role")]
            public static async Task Add(InteractionContext ctx,
            [Option("Role", "Role to add as autorole")]
            DiscordRole role,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(true)]
            bool visible = true)
            {
                DiscordGuild guild = ctx.Guild;

                AutoroleManager.Autorole autorole = new AutoroleManager.Autorole
                {
                    RoleID = role.Id.ToString()
                };

                await Task.Run(() =>
                {
                    AutoroleManager.WriteEntry(autorole, guild.Id.ToString());
                });

                await ctx.CreateResponseAsync($"Automatic role {role.Mention} has been added", !visible);
            }

            [SlashCommand("Remove", "Remove automatic role")]
            public static async Task Remove(InteractionContext ctx,
            [Option("Role", "Role to remove as autorole")]
            DiscordRole role,

            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(true)]
            bool visible = true)
            {
                DiscordGuild guild = ctx.Guild;

                await AutoroleManager.RemoveEntry(role.Id.ToString(), guild.Id.ToString());

                await ctx.CreateResponseAsync($"Automatic role {role.Mention} has been removed", !visible);
            }

            [SlashCommand("List", "List automatic roles on the server")]
            public static async Task List(InteractionContext ctx,
            [Option("Visible", "Sets the visibility", true)]
            [DefaultValue(false)]
            bool visible = false)
            {
                DiscordGuild guild = ctx.Guild;

                List<AutoroleManager.Autorole>? roles = AutoroleManager.ReadEntries(guild.Id.ToString());

                if (roles == null || !roles.Any())
                {
                    await ctx.CreateResponseAsync("Server has no autoroles");
                    return;
                }

                string messageText = "";

                foreach (AutoroleManager.Autorole autorole in roles)
                {
                    if (!ulong.TryParse(autorole.RoleID, out ulong id))
                    {
                        break;
                    }

                    messageText += $"{guild.GetRole(id).Mention}\n";
                }

                DiscordEmbedBuilder embded = new DiscordEmbedBuilder();

                embded.WithColor(DiscordColor.Purple);
                embded.WithTitle($"Autoroles in the {guild.Name} server");
                embded.WithDescription(messageText);

                await ctx.CreateResponseAsync(embded, !visible);
            }
        }
        #endregion
    }
}