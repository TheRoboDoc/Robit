using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace Robit
{
    public class Commands : BaseCommandModule
    {
        [Command("ping")]
        [Description("Pings the bot, if the bot is working correctly it responds with \"pong\"")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong");
        }

        [Command("join")]
        [Description("Makes the bot join the voice channel you are at")]
        public async Task Join(CommandContext ctx)
        {
            LavalinkExtension lava = ctx.Client.GetLavalink();

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();

            DiscordChannel channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined {channel.Name}!");
        }

        [Command("leave")]
        [Description("Makes the bot leave the voice channel you are at")]
        public async Task Leave(CommandContext ctx)
        {
            LavalinkExtension lava = ctx.Client.GetLavalink();

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();

            DiscordChannel channel = ctx.Member.VoiceState.Channel;

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            LavalinkGuildConnection conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }

        [Command("play")]
        [Description("Plays a given music track")]
        public async Task Play(CommandContext ctx, [RemainingText][Description("A search term or a direct link")] string search)
        {
            if (string.IsNullOrWhiteSpace(search) || string.IsNullOrEmpty(search))
            {
                await ctx.RespondAsync("Please provide a search term or a link");
                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            await Join(ctx);

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                Uri.TryCreate(search, UriKind.RelativeOrAbsolute, out Uri uri);
                loadResult = await node.Rest.GetTracksAsync(uri);

                if(loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.RespondAsync($"Track search failed for {search}.");
                    return;
                }
            }

            LavalinkTrack track = loadResult.Tracks.First();

            await conn.PlayAsync(track);

            await ctx.RespondAsync($"Now playing {track.Title}!");
        }

        [Command("pause")]
        [Description("Pauses the currently playing song")]
        public async Task Pause(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }
    }

    /// <summary>
    /// Help command stuff
    /// </summary>
    public class CustomHelpFormatter : DefaultHelpFormatter
    {
        public CustomHelpFormatter(CommandContext ctx) : base(ctx)
        {
            
        }

        public override CommandHelpMessage Build()
        {
            EmbedBuilder.Color = DiscordColor.Purple;
            return base.Build();
        }
    }
}
