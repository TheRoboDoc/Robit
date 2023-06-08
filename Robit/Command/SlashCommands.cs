using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Robit.Response;
using System.ComponentModel;
using static Robit.FileManager;

namespace Robit.Command
{
    internal class SlashCommands : ApplicationCommandModule
    {
        #region Technical
        [SlashCommand("Ping", "Pings the bot, the bot responds with the ping time in milliseconds")]
        public async Task Ping(InteractionContext ctx,

        [Option("Times", "Amount of times the bot should be pinged (Max 3)")]
        [DefaultValue(1)]
        [Maximum(3)]
        double times = 1,

        [Option("Visible", "Is the ping visible to others")]
        [DefaultValue(true)]
        bool visible = true)
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
        public async Task Commands(InteractionContext ctx)
        {
            SlashCommandsExtension slashCommandsExtension = Program.botClient.GetSlashCommands();

            List<KeyValuePair<ulong?, IReadOnlyList<DiscordApplicationCommand>>> slashCommandKeyValuePairs = slashCommandsExtension.RegisteredCommands.ToList();

            IReadOnlyList<DiscordApplicationCommand> slashCommands = slashCommandKeyValuePairs.FirstOrDefault().Value;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Title = "List of commands",
                Color = DiscordColor.Purple,
                Timestamp = DateTimeOffset.Now
            };

            foreach (DiscordApplicationCommand slashCommand in slashCommands)
            {
                string nameRaw = slashCommand.Name;
                string descriptionRaw = slashCommand.Description;

                string name = char.ToUpper(nameRaw[0]) + nameRaw.Substring(1);
                string description = char.ToUpper(descriptionRaw[0]) + descriptionRaw.Substring(1);

                embed.AddField(name, description);
            }

            await ctx.CreateResponseAsync(embed, true);
        }
        #endregion

        #region Interaction
        #region Introduction
        [SlashCommand("Intro", "Bot introduction")]
        public async Task Intro(InteractionContext ctx)
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

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("Github", "Posts a link to Robit's GitHub repo")]
        public async Task GitHub(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("https://github.com/TheRoboDoc/Robit", true);
        }
        #endregion

        #region Automatic Responses
        [SlashCommandGroup(
        "Response",
        "Add, remove, or edit bot's responses to messages. Needs permissions to manage emojis")]
        [SlashCommandPermissions(Permissions.ManageEmojis | Permissions.AddReactions)]
        public class Response
        {
            [SlashCommand("Add", "Add a response interaction")]
            public async Task Add(InteractionContext ctx,

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
            public async Task Remove(InteractionContext ctx,

            [Option("Name", "Name of the response interaction to delete")]
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
            public async Task Modify(InteractionContext ctx,

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
            public async Task List(InteractionContext ctx,
                [Option("Visible", "Sets the commands visibility", true)]
                [DefaultValue(false)]
                bool visible = false)
            {
                List<ResponseManager.ResponseEntry>? responseEntries = new List<ResponseManager.ResponseEntry>();

                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder();

                discordEmbedBuilder.Title = "List of all responses";
                discordEmbedBuilder.Color = DiscordColor.Purple;

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

                    foreach (ResponseManager.ResponseEntry responseEntry in responseEntries)
                    {
                        discordEmbedBuilder.AddField
                        (
                            responseEntry.reactName,
                            $"{responseEntry.content}: {responseEntry.response}"
                        );
                    }
                });

                await ctx.CreateResponseAsync(discordEmbedBuilder, !visible);
            }

            [SlashCommand("Wipe", "Wipe all of the response interactions")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task Wipe(InteractionContext ctx,
                [Option("visible", "Sets the visibility", true)]
                [DefaultValue(false)]
                bool visible = false)
            {
                List<ResponseManager.ResponseEntry> responseEntries = new List<ResponseManager.ResponseEntry>();

                ResponseManager.OverwriteEntries(responseEntries, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync("All response interactions have been overwritten", !visible);
            }

            [SlashCommand("Ignore", "Should Robit's auto response ignore this channel or not")]
            [SlashCommandPermissions(Permissions.ManageChannels | Permissions.ManageMessages)]
            public async Task Ignore(InteractionContext ctx,
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
        public async Task Convert(InteractionContext ctx,
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
        public async Task TagVoice(InteractionContext ctx,
            [Option("Message", "Message to ping people in current voice chat with")]
            [DefaultValue("")]
            string message = "",
            [Option("Attachment", "File attachment to the ping message")]
            [DefaultValue(null)]
            DiscordAttachment? attachment = null)
        {
            if (ctx.Member.VoiceState?.Channel == null)
            {
                await ctx.CreateResponseAsync("You must be in a voice chat", true);
                return;
            }

            DiscordChannel voiceChannel = ctx.Member.VoiceState.Channel;

            if (message == "" && attachment == null)
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

            content += $"\n{$"{ctx.Member.Mention} wanted people in '{voiceChannel.Name}' to see this:\n"}";

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
        public async Task Text(InteractionContext ctx,
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
                builder.WithContent(response);
            }
            else
            {
                builder.WithContent("**System:** " + response);
            }

            await ctx.EditResponseAsync(builder);
        }

        [SlashCommand("AI_Ignore", "Should Robit's AI module ignore this channel, prompt command will still work")]
        [SlashCommandPermissions(Permissions.ManageChannels | Permissions.ManageMessages)]
        public async Task AIIgnore(InteractionContext ctx,
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
            [SlashCommand("Number", "Generates a psudorandom number within given range")]
            public async Task Number(InteractionContext ctx,
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

            [SlashCommand("Dice", "Roll dice")]
            public async Task Dice(InteractionContext ctx,
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
                    diceResult = diceResult + $"{rolledValue} ";
                }

                int sum = rolledValues.Sum();
                int average = sum / rolledValues.Count();
                int min = rolledValues.Min();
                int max = rolledValues.Max();


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
                    embed.AddField("Min", $"{min}");
                    embed.AddField("Max", $"{max}");
                }

                await ctx.CreateResponseAsync(embed, !visible);
            }
        }
        #endregion

        #region Quotes
        public struct QuoteEntry
        {
            public string quote { get; set; }
            public string author { get; set; }
            public string bookSource { get; set; }
        }

        List<QuoteEntry>? quoteEntries;

        [SlashCommand("wh40kquote", "Fetches a random Warhammer 40k quote")]
        [SlashCommandPermissions(Permissions.SendMessages)]
        public async Task WH40kQuote(InteractionContext ctx)
        {
            string path = $"{Paths.resources}/Wh40ImperialQuotes.json";

            if (!FileExists(path))
            {
                CreateFile(path);
            }

            if (quoteEntries == null)
            {
                string jsonString = File.ReadAllText(path);

                try
                {
                    quoteEntries = JsonConvert.DeserializeObject<List<QuoteEntry>>(jsonString);
                }
                catch (Exception ex)
                {
                    Program.botClient?.Logger.LogWarning(ex.Message);

                    await ctx.CreateResponseAsync("Failed to fetch Warhammer 40k quote", true);

                    return;
                }
            }

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

            await ctx.CreateResponseAsync(embedBuilder);
        }
        #endregion

        #endregion
    }
}