using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace Robit
{
    public class Commands : BaseCommandModule
    {

        /// <summary>
        /// Ping command
        /// </summary>
        /// <param name="ctx">The command context</param>
        [Command("ping")]
        [Description
            (
            "Pings the bot, if the bot is working correctly it responds with \"pong\" " +
            "and the amount of milliseconds it took to respond"
            )]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync($"Pong {ctx.Client.Ping}ms");
        }

        /// <summary>
        /// Help command stuff
        /// </summary>
        public class CustomHelpFormatter : DefaultHelpFormatter
        {
            public CustomHelpFormatter(CommandContext ctx) : base(ctx)
            {
                context = ctx;
            }

            CommandContext context;

            public override CommandHelpMessage Build()
            {
                EmbedBuilder.Description = 
                    $"{context.Client.CurrentUser.Username}'s prefix commands are for debug purposes only. " +
                    $"You should use slash (\"/\") commands instead.";
                EmbedBuilder.Color = DiscordColor.Purple;
                return base.Build();
            }
        }
    }
}
