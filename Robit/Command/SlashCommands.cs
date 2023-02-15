using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using OpenAI.GPT3.ObjectModels;
using System.ComponentModel;

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
                $"slash command menu. I can respond to messages I'm pinged in, using OpenAI Davinci Text V3. " +
                $"Make sure to mention me and not my role as I will ignore those mentiones",

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
            string name,

            [Option("Content", "The content of the message to respond to")]
            string content,

            [Option("Response", "The response to the message")]
            string response)
            {
                FileManager.ResponseManager.ResponseEntry responseEntry = new FileManager.ResponseManager.ResponseEntry()
                {
                    reactName = name,
                    content = content,
                    response = response
                };

                List<FileManager.ResponseManager.ResponseEntry> allResponseEntries;
                allResponseEntries = FileManager.ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

                foreach (FileManager.ResponseManager.ResponseEntry entry in allResponseEntries)
                {
                    if (entry.reactName.ToLower() == responseEntry.reactName.ToLower())
                    {
                        await ctx.CreateResponseAsync("A response with a same name already exists");
                        return;
                    }
                }

                FileManager.ResponseManager.WriteEntry(responseEntry, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync($@"Added new response with trigger '{content}' and response '{response}'");
            }


            [SlashCommand("Remove", "Remove a response interaction by a given name")]
            public async Task Remove(InteractionContext ctx,

            [Option("Name", "Name of the response interaction to delete")]
            string name)
            {
                if (FileManager.ResponseManager.RemoveEntry(name, ctx.Guild.Id.ToString()))
                {
                    await ctx.CreateResponseAsync($@"Entry with a name {name} has been removed");
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find a response with a name {name}");
                }
            }


            [SlashCommand("Modify", "Modify a response")]
            public async Task Modify(InteractionContext ctx,

            [Option("Name", "Name of the response interaction to modify")]
            string name,

            [Option("Content", "New content for the interaction")]
            string content,

            [Option("Response", "New response for the interaction")]
            string response)
            {
                if (FileManager.ResponseManager.ModifyEntry(name, content, response, ctx.Guild.Id.ToString()).Result)
                {
                    await ctx.CreateResponseAsync
                        (
                            $@"Entry with a name '{name}' has been modified, " +
                            $@"it now responds to messages that contain '{content}' " +
                            $@"with '{response}'"
                        );
                }
                else
                {
                    await ctx.CreateResponseAsync($@"Couldn't find response interaction with a name {name}");
                }
            }


            [SlashCommand("List", "List all the response interactions")]
            public async Task List(InteractionContext ctx)
            {
                List<FileManager.ResponseManager.ResponseEntry> responseEntries = new List<FileManager.ResponseManager.ResponseEntry>();

                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder();

                discordEmbedBuilder.Title = "List of all responses";
                discordEmbedBuilder.Color = DiscordColor.Purple;

                await Task.Run(() =>
                {
                    responseEntries = FileManager.ResponseManager.ReadEntries(ctx.Guild.Id.ToString());

                    foreach (FileManager.ResponseManager.ResponseEntry responseEntry in responseEntries)
                    {
                        discordEmbedBuilder.AddField
                        (
                            responseEntry.reactName,
                            $"{responseEntry.content}: {responseEntry.response}"
                        );
                    }
                });

                await ctx.CreateResponseAsync(discordEmbedBuilder);
            }

            [SlashCommand("Wipe", "Wipe all of the response interactions")]
            [SlashCommandPermissions(Permissions.Administrator)]
            public async Task Wipe(InteractionContext ctx)
            {
                List<FileManager.ResponseManager.ResponseEntry> responseEntries = new List<FileManager.ResponseManager.ResponseEntry>();

                FileManager.ResponseManager.OverwriteEntries(responseEntries, ctx.Guild.Id.ToString());

                await ctx.CreateResponseAsync("All response interactions have been overwritten");
            }
        }
        #endregion

        #region Media Convert
        public enum FileFormats
        {
            mp4,
            mov,
            mkv,
            webm,
            gif,
            jpg,
            png,
            wepb,
            tiff
        }

        [SlashCommand("Convert", "Converts a given file from one format to another")]
        public async Task Convert(InteractionContext ctx, 
            [Option("Media_file", "Media file to convert from")]DiscordAttachment attachment,
            [Option("Format", "Format to convert to")]FileFormats fileFormat)
        {
            string[] mediaType = attachment.MediaType.Split('/');

            string type = mediaType[0];
            string format = mediaType[1];

            if(type != "image" && type != "video")
            {
                await ctx.CreateResponseAsync($"The given file format is '{type}' and not an image or an video", true);
                return;
            }

            if(format == fileFormat.GetName())
            {
                await ctx.CreateResponseAsync($"You tried to convert an '{format}' into an '{fileFormat.GetName()}'", true);
                return;
            }

            if(type == "video")
            {
                switch(fileFormat.GetName())
                {
                    case "jpg":
                    case "png":
                    case "wepb":
                    case "tiff":
                        await ctx.CreateResponseAsync($"You tried to convert a video into an image. " +
                            $"{ctx.Guild.CurrentMember.DisplayName} doesn't support turning video into image sequences", true);
                        return;
                }
            }
            else if(type == "image")
            {
                switch(fileFormat.GetName())
                {
                    case "mp4":
                    case "mov":
                    case "mkv":
                    case "webm":
                        await ctx.CreateResponseAsync($"You tried to convert an image into a video", true);
                        return;
                }
            }

            if(attachment.FileSize > 8388608)
            {
                await ctx.CreateResponseAsync($"Sorry but the file size was above 8 Mb ({attachment.FileSize / 1048576} Mb)", true);
                return;
            }

            if(type == "video" && fileFormat == FileFormats.gif)
            {
                await ctx.CreateResponseAsync("Creating gifs from video is an experemental feature, the bot might get stuck\n" +
                    "Processing...", true);
            }
            else
            {
                await ctx.CreateResponseAsync("Processing...", true);
            }

            FileManager.MediaManager.SaveFile(attachment.Url, ctx.Channel.Id.ToString(), format).Wait();

            await FileManager.MediaManager.Convert(ctx.Channel.Id.ToString(), format, fileFormat.GetName());

            string path = $"{FileManager.MediaManager.IDToPath(ctx.Channel.Id.ToString())}/output.{fileFormat.GetName()}";

            FileInfo fileInfo = new FileInfo(path);

            if(fileInfo.Length > 8388608)
            {
                await ctx.CreateResponseAsync($"Sorry but the resulting file was above 8 Mb ({attachment.FileSize / 1048576} Mb)", true);
                return;
            }

            FileStream fileStream = File.OpenRead(path);

            CompletionCreateResponse completionResult = await Program.openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
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

            string responseText = $"{ctx.Member.Mention}";

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

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

            builder.AddMention(UserMention.All);
            builder.WithContent(responseText);
            builder.AddFile(fileStream);

            await ctx.EditResponseAsync(builder);

            fileStream.Close();

            await FileManager.MediaManager.ClearChannelTempFolder(ctx.Channel.Id.ToString());
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

            if(message == "" && attachment == null)
            {
                await ctx.CreateResponseAsync("Message and attachment cannot both be empty", true);
                return;
            }

            string content = "";

            foreach (DiscordMember user in voiceChannel.Users)
            {
                if(!user.IsBot && user != ctx.Member)
                {
                    content += $"{user.Mention} ";
                }
            }

            content += $"\n{$"{ctx.Member.Mention} wanted people in '{voiceChannel.Name}' to see this:\n"}";

            if (message != "")
            {
                content += $"\n{message}";
            }

            if(attachment != null)
            {
                content += $"\n{attachment.Url}";
            }
            
            DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder();

            responseBuilder.AddMention(UserMention.All);
            responseBuilder.WithContent(content);

            await ctx.CreateResponseAsync(responseBuilder);
        }
        #endregion

        #endregion
    }
}
