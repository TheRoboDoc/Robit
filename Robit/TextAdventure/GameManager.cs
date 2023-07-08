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
                participantNameList += $"{SpecialCharacterRemoval(player.DisplayName)}\n";
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

        /// <summary>
        /// Creates an AI response
        /// </summary>
        /// <param name="chatMessages">Array of messages for the AI to have context on</param>
        /// <returns>A string containing the response generated by the AI</returns>
        /// <exception cref="NullReferenceException">
        /// AI error where either the whole service in not setup or there was an error generating the AI response
        /// </exception>
        private async Task<string> CreateResponse(ChatMessage[] chatMessages)
        {
            if (Program.OpenAiService == null)
            {
                Program.BotClient?.Logger.LogError(Response.AI.AIEvent, "OpenAI service isn't on");

                throw new NullReferenceException("OpenAiService is null");
            }

            ChatCompletionCreateResponse completionResult = await Program.OpenAiService.ChatCompletion.CreateCompletion(
            new ChatCompletionCreateRequest
            {
                Messages = chatMessages,
                Model = "gpt-3.5-turbo-16k",
                N = 1,
                User = players.First().Id.ToString(),
                Temperature = 0.5f,
                FrequencyPenalty = 1.1f,
                PresencePenalty = 0.8f
            });

            string response;

            if (completionResult.Successful)
            {
                response = completionResult.Choices.First().Message.Content;

                if (await AICheck(response))
                {
                    return "**Filtered**";
                }

                Tuple<bool, string?> filter = Check(response);

                if (filter.Item1)
                {
                    return "**Filtered**";
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.BotClient?.Logger.LogError(Response.AI.AIEvent, "{ErrorCode}: {ErrorMessage}", 
                    completionResult.Error.Code, completionResult.Error.Message);

                return $"System: OpenAI error {completionResult.Error.Code}: {completionResult.Error.Message}";
            }

            return response;
        }
        {
            List<ChatMessage> messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(gameStartingParameters)
            };

            List<DiscordMessage> discordMessages = await FetchMessages();
        }
    }
}
