using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.ComponentModel;

namespace Robit
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

            foreach(DiscordApplicationCommand slashCommand in slashCommands)
            {
                string nameRaw = slashCommand.Name;
                string descriptionRaw = slashCommand.Description;

                string name = char.ToUpper(nameRaw[0]) + nameRaw.Substring(1);
                string description = char.ToUpper(descriptionRaw[0]) + descriptionRaw.Substring(1);

                embed.AddField(name, description);
            }

            await ctx.CreateResponseAsync(embed, true);
        }

        #region Interaction
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

                foreach(FileManager.ResponseManager.ResponseEntry entry in allResponseEntries)
                {
                    if(entry.reactName.ToLower() == responseEntry.reactName.ToLower())
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
                if(FileManager.ResponseManager.ModifyEntry(name, content, response, ctx.Guild.Id.ToString()))
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
    }
}
