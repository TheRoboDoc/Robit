using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;

namespace Robit
{
    internal class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("Ping", "Pings the bot, if the bot is working correctly it responds with \"pong\"")]
        public async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync($"Pong {ctx.Client.Ping}ms");
        }

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
        public async Task Play(InteractionContext ctx, [Option("Search", "A search term or a direct link")]string search)
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

            LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                Uri.TryCreate(search, UriKind.RelativeOrAbsolute, out Uri uri);
                loadResult = await node.Rest.GetTracksAsync(uri);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    //await ctx.CreateResponseAsync($"Track search failed for {search}.");

                    DiscordFollowupMessageBuilder follwoUpMessage = new DiscordFollowupMessageBuilder()
                    {
                        Content = $"Track search failed for {search}."
                    };

                    await ctx.FollowUpAsync(follwoUpMessage);
                    return;
                }
            }

            LavalinkTrack track = loadResult.Tracks.First();

            await conn.PlayAsync(track);

            //await ctx.CreateResponseAsync($"Now playing {track.Title}!");

            DiscordFollowupMessageBuilder follwoUpMessage2 = new DiscordFollowupMessageBuilder()
            {
                Content = $"Now playing {track.Title}!"
            };

            await ctx.FollowUpAsync(follwoUpMessage2);
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
    }
}
