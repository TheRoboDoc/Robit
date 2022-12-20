using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using System.ComponentModel;
using System.Text.Json;

namespace Robit
{
    internal class SlashCommands : ApplicationCommandModule
    {
        #region Technical
        [SlashCommand("Ping", "Pings the bot, the bot responds with the ping time in milliseconds")]
        public async Task Ping(InteractionContext ctx, [Option("Times", "Amount of times the bot should be pinged (Max 3)")][DefaultValue(1)][Maximum(3)] double times = 1)
        {
            await ctx.CreateResponseAsync($"Pong {ctx.Client.Ping}ms");
            times--;

            for (int i = 0; times > i; times--)
            {
                DiscordFollowupMessageBuilder followUp = new DiscordFollowupMessageBuilder()
                {
                    Content = $"Pong {ctx.Client.Ping}ms"
                };

                await ctx.FollowUpAsync(followUp);
            }
        }
        #endregion

        #region Voice

        [SlashCommand("Join", "Makes the bot join the voice channel you are at")]
        public async Task Join(InteractionContext ctx)
        {
            LavalinkExtension lava = ctx.Client.GetLavalink();

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync("The Lavalink connection is not established");
                return;
            }

            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();

            DiscordChannel channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.CreateResponseAsync("Not a valid voice channel.");
                return;
            }

            await node.ConnectAsync(channel);
            await ctx.CreateResponseAsync($"Joined {channel.Name}!");
        }

        [SlashCommand("Leave", "Makes the bot leave the voice channel you are at")]
        public async Task Leave(InteractionContext ctx)
        {
            LavalinkExtension lava = ctx.Client.GetLavalink();

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync("The Lavalink connection is not established");
                return;
            }

            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();

            DiscordChannel channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.CreateResponseAsync("Not a valid voice channel.");
                return;
            }

            LavalinkGuildConnection conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.CreateResponseAsync($"Left {channel.Name}!");
        }



        [SlashCommand("Play", "Plays a given music track")]
        public async Task Play(InteractionContext ctx, [Option("Search", "A search term or a direct link")] string search)
        {
            if (string.IsNullOrWhiteSpace(search) || string.IsNullOrEmpty(search))
            {
                await ctx.CreateResponseAsync("Please provide a search term or a link");
                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync("You are not in a voice channel.");
                return;
            }

            await Join(ctx);

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                //await ctx.CreateResponseAsync("Lavalink is not connected.");

                DiscordFollowupMessageBuilder follwoUpMessage = new DiscordFollowupMessageBuilder()
                {
                    Content = "Lavalink is not connected"
                };

                await ctx.FollowUpAsync(follwoUpMessage);
                return;
            }

            bool linkSearch = Uri.TryCreate(search, UriKind.Absolute, out Uri searchUri);

            LavalinkLoadResult loadResult;

            if (linkSearch)
            {
                loadResult = await node.Rest.GetTracksAsync(searchUri);
            }
            else
            {
                try
                {
                    loadResult = await node.Rest.GetTracksAsync(search);
                }
                catch
                {
                    loadResult = await node.Rest.GetTracksAsync($"ytsearch:{search}");
                }
            }

            List<LavalinkTrack> tracks = new List<LavalinkTrack>();

            if(loadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                tracks = loadResult.Tracks.ToList();
            }
            else
            {
                tracks.Add(loadResult.Tracks.First());
            }

            string fileName = $@"{AppDomain.CurrentDomain.BaseDirectory}\{conn.Channel}tracks.json";

            FileInfo fileInfo = new FileInfo(fileName);

            if (!fileInfo.Exists)
            {
                fileInfo.Create().Dispose();
            }

            string jsonString = File.ReadAllText(fileName);

            if (!string.IsNullOrEmpty(jsonString))
            {
                List<LavalinkTrack> previousTracks = JsonSerializer.Deserialize<List<LavalinkTrack>>(jsonString);

                foreach (LavalinkTrack previousTrack in previousTracks)
                {
                    tracks.Add(previousTrack);
                }
            }

            using FileStream fileStream = File.Open(fileName, FileMode.Open);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            await JsonSerializer.SerializeAsync(fileStream, tracks, options);
            await fileStream.DisposeAsync();

            if(conn.CurrentState.CurrentTrack == null)
            {
                conn.PlaybackFinished += async (sender, e) =>
                {
                    int index = tracks.IndexOf(e.Track);

                    if(index < 0)
                    {
                        index = 0;
                    }

                    try
                    {
                        await conn.PlayAsync(tracks[index + 1]);
                    }
                    catch
                    {
                        DiscordFollowupMessageBuilder follwoUpMessage2 = new DiscordFollowupMessageBuilder()
                        {
                            Content = "Playback complete"
                        };

                        await ctx.FollowUpAsync(follwoUpMessage2);

                        File.Delete(fileName);
                    }
                };

                conn.PlaybackStarted += async (sender, e) =>
                {
                    DiscordFollowupMessageBuilder follwoUpMessage2 = new DiscordFollowupMessageBuilder()
                    {
                        Content = $"Now playing {e.Track.Title}"
                    };

                    await ctx.FollowUpAsync(follwoUpMessage2);
                };

                await conn.PlayAsync(tracks.First());
            }
        }

        [SlashCommand("Pause", "Pauses the currently playing song")]
        public async Task Pause(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync("You are not in a voice channel.");
                return;
            }

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        [SlashCommand("Continue", "Continues the playback")]
        public async Task Continue(InteractionContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.CreateResponseAsync("You are not in a voice channel.");
                return;
            }

            LavalinkExtension laval = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = laval.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.CreateResponseAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.CreateResponseAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }
        #endregion

        #region Interaction
        [SlashCommand("Intro", "Bot introduction")]
        public async Task Intro(InteractionContext ctx)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = ctx.Client.CurrentUser.AvatarUrl.ToString(),
                    Name = $"{ctx.Client.CurrentUser.Username}#{ctx.Client.CurrentUser.Discriminator}"
                },

                Color = DiscordColor.Purple,

                Description =
                $"Hi I'm {ctx.Client.CurrentUser.Mention} nice meeting you\n" +
                $"I'm currently but a baby and because of that limited in my abilites. " +
                $"At the moment my main ability is to play music in a voice chat. You can try that if you want. " +
                $"Just type a '/' (yep I work with slash commands :3)" +
                $"I also answer simple questions, just \"@\" me in the question and I will try my best, I promise",

                Timestamp = DateTimeOffset.Now,

                Title = "IT'S ME!!!",
            }.AddField("GitHub", @"https://github.com/TheRoboDoc/Robit");

            await ctx.CreateResponseAsync(embed);
        }

        [SlashCommand("Github", "Posts a link to Robit's GitHub repo")]
        public async Task GitHub(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("https://github.com/TheRoboDoc/Robit");
        }
        #endregion
    }
}
