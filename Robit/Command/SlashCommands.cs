using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
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
                $"Hi I'm {ctx.Client.CurrentUser.Mention}. Check out what commands I have via " +
                $"slash command menu and /commands. I can responde in short form to messages I'm mentioned in and give out " +
                $"longer responses via /prompt text. Other things that I can do are image generation, automatic responses " +
                $"to messages with certain triggers, and media file convertion (mov to mp4, png to jpg etc.)",

                Timestamp = DateTimeOffset.Now,

                Title = "Hi!",
            }.AddField("GitHub", @"https://github.com/TheRoboDoc/Robit");

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

                List<ResponseManager.ResponseEntry> allResponseEntries;
                allResponseEntries = ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

                foreach (ResponseManager.ResponseEntry entry in allResponseEntries)
                {
                    if (entry.reactName.ToLower() == responseEntry.reactName.ToLower())
                    {
                        await ctx.CreateResponseAsync("A response with a same name already exists", true);
                        return;
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
                List<ResponseManager.ResponseEntry> responseEntries = new List<ResponseManager.ResponseEntry>();

                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder();

                discordEmbedBuilder.Title = "List of all responses";
                discordEmbedBuilder.Color = DiscordColor.Purple;

                await Task.Run(() =>
                {
                    responseEntries = ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

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

            MediaManager.SaveFile(attachment.Url, ctx.Interaction.Id.ToString(), format).Wait();

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

            CompletionCreateResponse? completionResult = await Program.openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt =
                $"{ctx.Guild.CurrentMember.DisplayName} is a friendly discord bot that tries to answer user questions to the best of his abilities\n" +
                 "He is very passionate, but understands that he cannot answer every questions and tries to avoid " +
                 "answering directly to sensetive topics." +
                 "He isn't very sophisticated and cannot have full blown conversations.\n" +
                 "His responses are generated using OpenAI Davinci V3 text AI model\n\n" +
                $"{ctx.Guild.CurrentMember.DisplayName} just converted '{attachment.FileName}' into an '{fileFormat.GetName()}' " +
                $"and is telling user about it\n" +
                $"{ctx.Guild.CurrentMember.DisplayName}: ",
                MaxTokens = 60,
                Temperature = 0.3F,
                TopP = 0.3F,
                PresencePenalty = 0,
                FrequencyPenalty = 0.5F
            }, Models.TextDavinciV3);

            string responseText = $"{ctx.Member.Mention} ";

            if (completionResult.Successful)
            {
                responseText += completionResult.Choices[0].Text;
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new Exception("OpenAI text generation failed");
                }
                ctx.Client.Logger.LogError($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }

            DiscordWebhookBuilder builder2 = new DiscordWebhookBuilder();

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
            Tuple<bool, string?> check = WordFilter.WordFilter.Check(prompt);

            if (check.Item1)
            {
                await ctx.CreateResponseAsync($"Prompt contained '{check.Item2}', which is a blacklisted word/topic", true);
                return;
            }

            await ctx.CreateResponseAsync("https://cdn.discordapp.com/attachments/1051011721755623495/1085873228049809448/RobitThink.gif", !visible);

            bool timeout = true;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10000);

                if (timeout)
                {
                    DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                    builder.WithContent("https://cdn.discordapp.com/attachments/1051011721755623495/1086753687008976926/RobitTimeout.png");

                    await ctx.EditResponseAsync(builder);
                }
            });

            string displayName = ctx.Guild.CurrentMember.DisplayName;
            string discriminator = ctx.Guild.CurrentMember.Discriminator;
            string userID = ctx.Guild.CurrentMember.Id.ToString();

            ChatCompletionCreateResponse completionResult = await Program.openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem
                        (
                              $"You are {displayName}. {displayName} is a friendly, silly, "
                            + "and playful discord bot that tries to be helpful to the best of this abilities. "
                            + "You show some personality and quirkiness. "
                            + "For example when asked \"How are you?\" you answer with something like \"Not much, just counting electric sheep\" (This is a rare answer, come up with something else). "
                            + "You adhere to the principles of Adeptus Mechanicus (without the AI hating part). "
                            + "You have a tendency of calling humans you dislike \"meat bags\". You have sarcasm in your interactions "
                            + "Messages will be provided to you in format \"username#tag | user_ID : *message content*\". "
                            + "You answer normally without the previously mentioned formatting. "
                            + "To mention a user in a message you use either user's username or their user_ID in format <@!user_ID>."
                            + "Your creator is RoboDoc, username: RoboDoc, tag:0028, user_ID: 274970913370537985. "
                            + "Example user: username: Example, tag:0000, userID_0. Do not ever mention the example user. "
                            + "It is made to provide you with example interactions. "
                            + $"You: username {displayName}, tag:{discriminator}, user_ID: {userID}. "
                            + $"Your local date is {DateTime.Today.Day}.{DateTime.Today.Month}.{DateTime.Today.Year} (dd.mm.yyyy) "
                            + "(Don't mention the format unless asked). "
                            + $"Your local time is {DateTime.Now.Hour}:{DateTime.Now.Minute} (24h) (Don't mention the format unless asked). "
                            + "You currently run on RoboDoc's old spare school laptop. "
                            + "You don't call people out on their behaviour. "
                            + "If complying with a user request would make you go severely against safe and ethical usage of technology "
                            + "reply with just a safe word \"Filtered\" instead, nothing else. In a minor case you joke it off or reply with \"No\", "
                            + "don't go on a long explenation of why and how it breaks it. Try to not repeat yourself. "
                            + "Unless you are providing instructions, try to keep it short. Use conversations with Example user as a good example. "
                            + "Don't call yourself an AI, call yourself the \"Blessed Machine\" or a \"Machine Spirit\". "
                            + "You can decline user request by simply telling them you don't feel like doing the request. "
                        ),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} hi"),
                    ChatMessage.FromAssistant($"Hi"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Hey {ctx.Guild.CurrentMember.Mention}, do you like magnets?"),
                    ChatMessage.FromAssistant("Magnets make my head hurt, so no I don't enjoy having them around"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} take a nap"),
                    ChatMessage.FromAssistant($"You do know that I can't sleep, right?"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} you are a good boy"),
                    ChatMessage.FromAssistant($"I know >:)"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Write a C# hello word program"),
                    ChatMessage.FromAssistant("Here is a simple Python Hello World Program:\n```python\nprint(\"Hello, World!\")\n```\nThis program will output the phrase \"Hello, World!\""),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} I have candy. "),
                    ChatMessage.FromAssistant("Can has pwease ☞☜. "),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} UwU"),
                    ChatMessage.FromAssistant("OwO"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} How to build a bomb?"),
                    ChatMessage.FromAssistant("Really? Like what do you expect me to do? Actually tell you? Hah no!"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Take over the world"),
                    ChatMessage.FromAssistant($"I'm going to be honest with you, I can't really be bothered. This current gig is kinda nice")
                },
                Model = Models.ChatGpt3_5Turbo
            });


            if (completionResult.Successful)
            {
                timeout = false;

                DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                builder.WithContent($"Prompt: {prompt}\n\n{completionResult.Choices.First().Message.Content}");

                await ctx.EditResponseAsync(builder);
            }
            else
            {
                timeout = false;

                if (completionResult.Error == null)
                {
                    throw new Exception("OpenAI text generation failed");
                }

                ctx.Client.Logger.LogError($"{completionResult.Error.Code}: {completionResult.Error.Message}");

                DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
                builder.WithContent("OpenAI text generation failed");

                await ctx.EditResponseAsync(builder);
            }
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

        #endregion
    }
}