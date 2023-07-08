using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OpenAI.ObjectModels.RequestModels;

namespace Robit.TextAdventure
{
    /// <summary>
    /// Game manager responsible for running a text based adventure game instance
    /// </summary>
    public class GameManager
    {
        /// <summary>
        /// Array of players in the game
        /// </summary>
        private DiscordMember[] players;

        /// <summary>
        /// Current turn count
        /// </summary>
        private uint turnCount;

        /// <summary>
        /// Maximum turn count
        /// </summary>
        private uint maxTurnCount;

        /// <summary>
        /// Name of the text based adventure instance
        /// </summary>
        public string gameName { private set; get; }

        /// <summary>
        /// Starting parameters of the text based adventure instance
        /// </summary>
        private string gameStartingParameters;

        /// <summary>
        /// The discord thread this current text based adventure instance is happening at
        /// </summary>
        public DiscordThreadChannel channel { private set; get; }

        /// <summary>
        /// GameManager instance contructor
        /// </summary>
        /// <param name="players">The list of players participating</param>
        /// <param name="gameName">The name of the game instance</param>
        /// <param name="theme">The theme of the game</param>
        /// <param name="maxTurnCountPerPlayer">Max turn count per player</param>
        /// <param name="gameMode">Game mode of the game</param>
        /// <param name="channel">The thread channel this instance should be happening at</param>
        private GameManager(DiscordMember[] players, string gameName, string theme, 
                            uint maxTurnCountPerPlayer, DiscordThreadChannel channel)
        {
            this.players = players;
            this.gameName = gameName;
            this.channel = channel;

            turnCount = 0;

            maxTurnCount = (uint)(players.Length * maxTurnCountPerPlayer);

            string participantNameList = "";

            foreach (DiscordMember player in players)
            {
                participantNameList += $"{player.DisplayName}\n";
            }

            gameStartingParameters =
                $"Write a text based adventure game.\n" +
                $"Maximum amount of turns you can take: {maxTurnCount}\n" +
                $"The game always follows this event pattern:\n" +
                $"Description of the event -> Question to the player -> Player answer -> Result\n" +
                $"Each complete event equals one turn" +
                $"Participants are:\n" +
                $"{participantNameList}\n" +
                $"Theme as requested by players:\n" +
                $"{theme}";
        }

        /// <summary>
        /// Starts a new instance of a text based adventure game
        /// </summary>
        /// <param name="players">Players that will be participating</param>
        /// <param name="gameName">The name of the game instance</param>
        /// <param name="theme">The theme of the game instance</param>
        /// <param name="context">Interaction context</param>
        /// <param name="maxTurnCountPerPlayer">Maximum turn count per player</param>
        /// <param name="gameMode">Game instance game mode</param>
        /// <returns>Game manager instance that will be managing this instance of the text based adventure game</returns>
        public static async Task<GameManager> Start
            (DiscordMember[] players, string gameName, string theme, InteractionContext context, uint maxTurnCountPerPlayer)
        {
            DiscordThreadChannel channel = await context.Channel.CreateThreadAsync
                (gameName, DSharpPlus.AutoArchiveDuration.Day, DSharpPlus.ChannelType.PrivateThread, $"Text Based Adventure Game: {gameName}");

            return new GameManager(players, gameName, theme, maxTurnCountPerPlayer, channel);
        }

        /// <summary>
        /// Fetches all the messages that were involved in this current text based adventure game instance
        /// </summary>
        /// <returns>A list of discord messages</returns>
        private async Task<List<DiscordMessage>> FetchMessages()
        {
            IReadOnlyList<DiscordMessage> discordReadOnlyMessageList = await channel.GetMessagesAsync(int.MaxValue);

            List<DiscordMessage> discordMessages = new List<DiscordMessage>();

            await Task.Run(() =>
            {
                foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
                {
                    discordMessages.Add(discordMessage);
                }

                discordMessages.Reverse();
            });

            return discordMessages;
        }

        public async Task Run()
        {
            List<ChatMessage> messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(gameStartingParameters)
            };

            List<DiscordMessage> discordMessages = await FetchMessages();
        }
    }
}
