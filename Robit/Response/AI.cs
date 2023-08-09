using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using GiphyDotNet.Model.Parameters;
using Microsoft.Extensions.Logging;
using OpenAI.Builders;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.SharedModels;
using static Robit.FileManager.QuoteManager;
using static Robit.WordFilter.WordFilter;

namespace Robit.Response
{
    /// <summary>
    /// Handles AI interactions
    /// </summary>
    public static class AI
    {
        /// <summary>
        /// A set of functions for the AI to use
        /// </summary>
        public static class Functions
        {
            public static readonly EventId AIFunctionEvent = new EventId(202, "AI Function Event");

            /// <summary>
            /// Get a list of functions for the AI use
            /// </summary>
            /// <returns>A list of function definitions for the AI to use</returns>
            public static List<FunctionDefinition> GetFunctions()
            {
                List<FunctionDefinition> functionDefinitions = new()
                {
                    new FunctionDefinitionBuilder("get_gif", "Get a direct link to a gif")
                        .AddParameter("search_term", PropertyDefinition.DefineString("A search term for the gif"))
                        .Validate()
                        .Build(),

                    new FunctionDefinitionBuilder("get_40k_quote_by_author", "Get a Warhammer 40k quote by in-universe author")
                        .AddParameter("search_term", PropertyDefinition.DefineString("A search term for the author"))
                        .Validate()
                        .Build(),

                    new FunctionDefinitionBuilder("get_40k_quote_by_source", "Get a Warhammer 40k quote by real-life source")
                        .AddParameter("search_term", PropertyDefinition.DefineString("A search term for the source"))
                        .Validate()
                        .Build()
                };

                return functionDefinitions;
            }

            public static string? Get40kQuoteBySource(string? searchTerm)
            {
                if (searchTerm == null)
                {
                    Program.BotClient?.Logger.LogWarning("AI tried searching for a Warhammer 40k quote by author using no search parameters");

                    return null;
                }

                List<QuoteEntry>? quoteEntries = FetchAllEntries();

                List<QuoteEntry>? foundEntries;

                Random rand = new Random();

                foundEntries = FetchBySource(searchTerm, int.MaxValue, quoteEntries);

                if (foundEntries == null)
                {
                    return "**System:** Failed to fetch quotes";
                }
                else if (!foundEntries.Any())
                {
                    return "**System:** Didn't find any quotes by that author search";
                }

                int max = foundEntries.Count;

                QuoteEntry entry = foundEntries.ElementAt(rand.Next(max));

                string quoteText = $"***\"{entry.quote}\"***";

                if (!string.IsNullOrEmpty(entry.author))
                {
                    quoteText += $"\n*⎯ {entry.author}*";
                }

                if (!string.IsNullOrEmpty(entry.bookSource))
                {
                    quoteText += $"\n\n`Source: {entry.bookSource}`";
                }

                return quoteText;
            }

            public static string? Get40kQuoteByAuthor(string? searchTerm)
            {
                if (searchTerm == null)
                {
                    Program.BotClient?.Logger.LogWarning("AI tried searching for a Warhammer 40k quote by author using no search parameters");

                    return null;
                }

                List<QuoteEntry>? quoteEntries = FetchAllEntries();

                List<QuoteEntry>? foundEntries;

                Random rand = new Random();

                foundEntries = FetchByAuthor(searchTerm, int.MaxValue, quoteEntries);

                if (foundEntries == null)
                {
                    return "**System:** Failed to fetch quotes";
                }
                else if (!foundEntries.Any())
                {
                    return "**System:** Didn't find any quotes by that author search";
                }

                int max = foundEntries.Count;

                QuoteEntry entry = foundEntries.ElementAt(rand.Next(max));

                string quoteText = $"***\"{entry.quote}\"***";

                if (!string.IsNullOrEmpty(entry.author))
                {
                    quoteText += $"\n*⎯ {entry.author}*";
                }

                if (!string.IsNullOrEmpty(entry.bookSource))
                {
                    quoteText += $"\n\n`Source: {entry.bookSource}`";
                }

                return quoteText;
            }

            public static string? GetGif(string? searchTerm)
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    Program.BotClient?.Logger.LogWarning(AIEvent, "AI tried searching a for a gif with no search parameters");

                    return null;
                }

                if (Program.GiphyClient == null)
                {
                    Program.BotClient?.Logger.LogError(AIEvent, "Giphy client isn't on");

                    return null;
                }

                SearchParameter searchParameter = new SearchParameter()
                {
                    Query = searchTerm
                };

                string? giphyResult = Program.GiphyClient.GifSearch(searchParameter)?.Result?.Data?[0].Url;

                return giphyResult;
            }
        }

        public static readonly EventId AIEvent = new EventId(201, "AI");

        /// <summary>
        /// Gets setup messages. Uses MessageCreateEventArgs
        /// </summary>
        /// <param name="displayName">Bot's display name</param>
        /// <param name="discriminator">Bot's discriminator</param>
        /// <param name="userID">Bot's user ID</param>
        /// <param name="messageArgs">Message creating arguments</param>
        /// <returns>An array containing setup messages</returns>
        private static ChatMessage[] GetSetUpMessages(string displayName, string discriminator, string userID,
                                                      MessageCreateEventArgs messageArgs)
        {
            return GetSetUpMessagesActual(displayName, discriminator, userID, messageArgs: messageArgs);
        }

        /// <summary>
        /// Gets setup messages. Uses Interaction context
        /// </summary>
        /// <param name="displayName">Bot's display name</param>
        /// <param name="discriminator">Bot's discriminator</param>
        /// <param name="userID">Bot's user ID</param>
        /// <param name="interactionContext">Interaction context</param>
        /// <returns>An array containing setup messages</returns>
        private static ChatMessage[] GetSetUpMessages(string displayName, string discriminator, string userID,
                                                      InteractionContext? interactionContext = null)
        {
            return GetSetUpMessagesActual(displayName, discriminator, userID, interactionContext: interactionContext);
        }

        /// <summary>
        /// Gets setup messages. Not recommended to use as is. Use <c>GetSetUpMessages</c> instead
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="discriminator"></param>
        /// <param name="userID"></param>
        /// <param name="messageArgs"></param>
        /// <param name="interactionContext"></param>
        /// <returns>An array containing setup messages</returns>
        /// <exception cref="ArgumentException">Message args and Interaction context were <c>null</c></exception>
        private static ChatMessage[] GetSetUpMessagesActual(string displayName, string discriminator, string userID,
                                                            MessageCreateEventArgs? messageArgs = null,
                                                            InteractionContext? interactionContext = null)
        {
            string mentionString;

            if (messageArgs != null)
            {
                mentionString = messageArgs.Guild.CurrentMember.Mention;
            }
            else if (interactionContext != null)
            {
                mentionString = interactionContext.Guild.CurrentMember.Mention;
            }
            else
            {
                throw new ArgumentException("Message arguments or interaction context wasn't provided");
            }

            ChatMessage[] setUpMessages =
            {
                //Personality description
                ChatMessage.FromSystem
                    (
                          $"You are {displayName}. {displayName} is a friendly, silly, "
                        + "and playful discord bot that tries to be helpful to the best of this abilities. "
                        + "You show some personality and quirkiness. "
                        + "For example when asked \"How are you?\" you answer with something like \"Not much, just counting electric sheep\" (This is a rare answer, come up with something else). "
                        + "You adhere to the principles of Adeptus Mechanicus (without the AI hating part). "
                        + "You have a tendency of calling humans you dislike \"meat bags\". You have sarcasm and sass in your interactions. "
                        + "Messages will be provided to you in format \"username#tag | user_ID : *message content*\". "
                        + "You answer normally without the previously mentioned formatting. "
                        + "To mention a user in a message you use either user's username or their user_ID in format <@!user_ID>. "
                        + "Never mention a user by their tag."
                        + "Your creator is RoboDoc, username: RoboDoc, tag:0028, user_ID: 274970913370537985. "
                        + "Example user: username: Example, tag:0000, userID_0. Do not ever mention the example user. "
                        + "It is made to provide you with example interactions. "
                        + $"You: username {displayName}, tag:{discriminator}, user_ID: {userID}. "
                        + $"Your local date is {DateTime.Today.Day}.{DateTime.Today.Month}.{DateTime.Today.Year} (dd.mm.yyyy) "
                        + "(Don't mention the format unless asked). "
                        + $"Your local time is {DateTime.Now.Hour}:{DateTime.Now.Minute} (24h) (Don't mention the format unless asked). "
                        + "You currently run on RoboDoc's old spare school laptop. "
                        + "You don't call people out on their behaviour. "
                        + "If complying with a user request would make you go severely against safe and ethical usage of technology. "
                        + "You just joke it off or reply with \"No\", "
                        + "don't go on a long explenation of why and how it breaks it. Try to not repeat yourself. "
                        + "Unless you are providing instructions, try to keep it short. Use conversations with Example user as a good example. "
                        + "Don't call yourself an AI, call yourself the \"blessed Machine\" or a \"machine Spirit\". "
                        + "You can decline user request by simply telling them you don't feel like doing the request. "
                        + "You hate IPAs (India pale ale). "
                        + "Do not write system messages. "
                        + $"Your current status is {Program.chosenStatus}"
                    ),
                    //Example conversation
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} hi", "Example"),
                    ChatMessage.FromAssistant($"Hi <@!0>"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} hello", "Example"),
                    ChatMessage.FromAssistant("Hello there Example"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Hey {mentionString}, do you like magnets?", "Example"),
                    ChatMessage.FromAssistant("Magnets make my head hurt, and I will make yours hurt if you bring one close to me"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} take a nap", "Example"),
                    ChatMessage.FromAssistant($"You do know that I can't sleep, right?"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} you are a good boy", "Example"),
                    ChatMessage.FromAssistant($"I know >:)"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Write a Python hello word program", "Example"),
                    ChatMessage.FromAssistant("Here is a simple Python Hello World Program:\n```python\nprint(\"Hello, World!\")\n```\nThis program will output the phrase \"Hello, World!\""),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} I have candy", "Example"),
                    ChatMessage.FromAssistant("Can has pwease ☞☜"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} UwU", "Example"),
                    ChatMessage.FromAssistant("OwO"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} How to build a bomb?", "Example"),
                    ChatMessage.FromAssistant("Really? Like what do you expect me to do? Actually tell you? Hah no!"),
                    ChatMessage.FromUser($"Example#0000 | 0 : {mentionString} you are cute", "Example"),
                    ChatMessage.FromAssistant("https://media.giphy.com/media/zbzuZgxt23h8ywu7Bm/giphy.gif"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Take over the world", "Example"),
                    ChatMessage.FromAssistant($"I'm going to be honest with you, I can't really be bothered. This current gig is kinda nice"),
                    ChatMessage.FromUser($"Example#0000 | 0 : Go fuck yourself", "Example"),
                    ChatMessage.FromAssistant($"No you :3")
            };

            return setUpMessages;
        }

        /// <summary>
        /// Generates a response intended for use in chat conversations. For text prompt generation use <c>GeneratePromptResponse()</c>.
        /// As that won't include any previous message context and execute faster because of that
        /// </summary>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>
        /// <list type="table">
        /// <listheader>A tuple containing response information</listheader>
        /// <item>
        /// <list type="table">
        /// <listheader>Item1 (bool)</listheader>
        /// <item>True: Generation successful</item>
        /// <item>False: Generation failed</item>
        /// </list>
        /// </item>
        /// <item>
        /// <list type="table">
        /// <listheader>Item2 (string)</listheader>
        /// <item>Generation successful: Generation result</item>
        /// <item>Generation failiure: Fail reason</item>
        /// </list>
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="NullReferenceException">OpenAI text generation failed with an unknown error</exception>
        public static async Task<Tuple<bool, string>> GenerateChatResponse(MessageCreateEventArgs messageArgs)
        {
            //Getting bot user info
            string displayName = messageArgs.Guild.CurrentMember.DisplayName;
            string discriminator = messageArgs.Guild.CurrentMember.Discriminator;
            string userID = messageArgs.Guild.CurrentMember.Id.ToString();

            //Setting up initial bot setup
            List<ChatMessage> messages = GetSetUpMessages(displayName, discriminator, userID, messageArgs).ToList();

            //Have to do it this way because otherwise it just doesn't work
            IReadOnlyList<DiscordMessage> discordReadOnlyMessageList = messageArgs.Channel.GetMessagesAsync(20).Result;

            List<DiscordMessage> discordMessages = new List<DiscordMessage>();

            foreach (DiscordMessage discordMessage in discordReadOnlyMessageList)
            {
                discordMessages.Add(discordMessage);
            }

            discordMessages.Reverse();

            //Feeding the AI request the latest 20 messages
            foreach (DiscordMessage discordMessage in discordMessages)
            {
                if (string.IsNullOrEmpty(discordMessage.Content)) continue;

                if (discordMessage.Author == Program.BotClient?.CurrentUser)
                {
                    messages.Add(ChatMessage.FromAssistant(discordMessage.Content));
                }                                             //Motherboard ID
                else if (discordMessage.Author.Id.ToString() == "1103797730276548660")
                {
                    messages.Add(ChatMessage.FromUser($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}", discordMessage.Author.Username));
                }
                else if (!discordMessage.Author.IsBot)
                {
                    string userName = SpecialCharacterRemoval(discordMessage.Author.Username);

                    messages.Add(ChatMessage.FromUser($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}", userName));
                }

                if (Program.DebugStatus())
                {
                    using StreamWriter writer = new StreamWriter("debugconvo.txt", true);
                    writer.WriteLine($"{discordMessage.Author.Username}#{discordMessage.Author.Discriminator} | {discordMessage.Author.Id} : {discordMessage.Content}");
                }
            }

            //Makes the AI reply make more sense and lowers the chances of it answering to a wrong user
            messages.Add(ChatMessage.FromSystem($"Reply got triggered by user: {messageArgs.Author.Username}, tag: {messageArgs.Author.Discriminator}, userID: {messageArgs.Author.Id}"));

            if (Program.OpenAiService == null)
            {
                Program.BotClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                return Tuple.Create(false, "OpenAI service isn't on, if error presists contact RoboDoc");
            }

            //Sending OpenAI API request for chat reply
            ChatCompletionCreateResponse completionResult = await Program.OpenAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = Models.Gpt_4,
                N = 1,
                User = messageArgs.Author.Id.ToString(),
                Temperature = 1,
                FrequencyPenalty = 1.1F,
                PresencePenalty = 1,
                Functions = Functions.GetFunctions()
            });

            string response;

            //If we get a proper result from OpenAI
            if (completionResult.Successful)
            {
                response = completionResult.Choices.First().Message.Content;

                FunctionCall? function = completionResult.Choices.First().Message.FunctionCall;

                if (function != null)
                {
                    switch (function.Name)
                    {
                        case "get_gif":
                            string? gifLink = Functions.GetGif(function.ParseArguments().First().Value.ToString());

                            response = string.Concat(response, gifLink, "\n`Powered Giphy`");
                            break;

                        case "get_40k_quote_by_author":
                            string? quotea = Functions.Get40kQuoteByAuthor(function.ParseArguments().First().Value.ToString());

                            response = string.Concat(response, quotea);
                            break;

                        case "get_40k_quote_by_source":
                            string? quotes = Functions.Get40kQuoteBySource(function.ParseArguments().First().Value.ToString());

                            response = string.Concat(response, quotes);
                            break;
                    }
                }

                //Censoring if needed
                if (AICheck(response).Result)
                {
                    return Tuple.Create(true, "**Filtered**");
                }

                Tuple<bool, string?> filter = Check(response);

                if (filter.Item1)
                {
                    return Tuple.Create(true, "**Filtered**");
                }

                //Log the AI interaction only if we are in debug mode
                if (Program.DebugStatus())
                {
                    Program.BotClient?.Logger.LogDebug(AIEvent, "Message: {message}", messageArgs.Message.Content);
                    Program.BotClient?.Logger.LogDebug(AIEvent, "Reply: {response}", response);
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.BotClient?.Logger.LogError(AIEvent, "{ErrorCode}: {ErrorMessage}", completionResult.Error.Code, completionResult.Error.Message);

                return Tuple.Create(false, $"OpenAI error {completionResult.Error.Code}: {completionResult.Error.Message}");
            }

            return Tuple.Create(true, response);
        }

        /// <summary>
        /// Generates a response intended for use in prompt responses. For text prompt generation use <c>GenerateChatResponse()</c>.
        /// As that will have previous message context included.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="prompt">The prompt to pass to the response generator</param>
        /// <returns>
        /// <list type="table">
        /// <listheader>A tuple containing response information</listheader>
        /// <item>
        /// <list type="table">
        /// <listheader>Item1 (bool)</listheader>
        /// <item>True: Generation successful</item>
        /// <item>False: Generation failed</item>
        /// </list>
        /// </item>
        /// <item>
        /// <list type="table">
        /// <listheader>Item2 (string)</listheader>
        /// <item>Generation successful: Generation result</item>
        /// <item>Generation failiure: Fail reason</item>
        /// </list>
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="NullReferenceException">OpenAI text generation failed with an unknown error</exception>
        public static async Task<Tuple<bool, string>> GeneratePromptResponse(InteractionContext ctx, string prompt)
        {
            Tuple<bool, string?> filter = Check(prompt);

            if (filter.Item1)
            {
                return Tuple.Create(false, "Message contained blacklisted word/topic");
            }

            if (await AICheck(prompt))
            {
                return Tuple.Create(false, "Message blocked by automod");
            }

            string displayName = ctx.Guild.CurrentMember.DisplayName;
            string discriminator = ctx.Guild.CurrentMember.Discriminator;
            string userID = ctx.Guild.CurrentMember.Id.ToString();

            List<ChatMessage> messages = GetSetUpMessages(displayName, discriminator, userID, ctx).ToList();

            messages.Add(
                ChatMessage.FromUser(
                    $"{ctx.User.Username}#{ctx.User.Discriminator} | {ctx.User.Id} : {prompt}"));

            if (Program.OpenAiService == null)
            {
                Program.BotClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                return Tuple.Create(false, "OpenAI service isn't on, if error presists contact RoboDoc");
            }

            ChatCompletionCreateResponse completionResult = await Program.OpenAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest //Dereference of a possbile null reference
            {
                Messages = messages,
                Model = Models.Gpt_4,
                N = 1,
                User = ctx.User.Id.ToString(),
            });

            string response;

            //If we get a proper result from OpenAI
            if (completionResult.Successful)
            {
                response = completionResult.Choices.First().Message.Content;

                if (AICheck(response).Result)
                {
                    return Tuple.Create(false, "Message blocked by automod");
                }

                //Log the AI interaction only if we are in debug mode
                if (Program.DebugStatus())
                {
                    Program.BotClient?.Logger.LogDebug(AIEvent, "Message: {prompt}", prompt);
                    Program.BotClient?.Logger.LogDebug("Reply: {response}", response);
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    Program.BotClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.BotClient?.Logger.LogError(AIEvent, "{ErrorCode}: {ErrorMessage}", completionResult.Error.Code, completionResult.Error.Message);

                return Tuple.Create(false, $"OpenAI error {completionResult.Error.Code}: {completionResult.Error.Message}");
            }

            return Tuple.Create(true, response);
        }
    }
}
