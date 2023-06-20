﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using GiphyDotNet.Model.Parameters;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using System.Text.RegularExpressions;
using static Robit.WordFilter.WordFilter;

namespace Robit.Response
{
    /// <summary>
    /// Handles AI interactions
    /// </summary>
    public static class AI
    {
        public static readonly EventId AIEvent = new EventId(201, "AI");

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
            if (AICheck(messageArgs.Message.Content).Result)
            {
                return Tuple.Create(false, "Message blocked by automod");
            }

            //Getting bot user info
            string displayName = messageArgs.Guild.CurrentMember.DisplayName;
            string discriminator = messageArgs.Guild.CurrentMember.Discriminator;
            string userID = messageArgs.Guild.CurrentMember.Id.ToString();

            //Setting up initial bot setup
            List<ChatMessage> messages = new List<ChatMessage>()
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
                        + "To post a react gif write search term surrounded by \"§§\". Example gif of a cat: §cat§. "
                        + "If you search for a gif, you can't have anything added after the search terms. (Example: \"§cat§\" is valid, \"§cat§.\". is not valid"
                        + "Do not make up links for the gifs or by what gif provider it is powered by. Only use the square brackets. "
                        + "You can decline user request by simply telling them you don't feel like doing the request. "
                        + "You hate IPAs (India pale ale). "
                        + "Do not write system messages. "
                        + $"Your current status is {Program.chosenStatus}"
                    ),
                //Example conversation
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} hi", "Example"),
                ChatMessage.FromAssistant($"Hi <@!0>"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} hello", "Example"),
                ChatMessage.FromAssistant("Hello there Example"),
                ChatMessage.FromUser($"Example#0000 | 0 : Hey {messageArgs.Guild.CurrentMember.Mention}, do you like magnets?", "Example"),
                ChatMessage.FromAssistant("Magnets make my head hurt, and I will make yours hurt if you bring one close to me"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} take a nap", "Example"),
                ChatMessage.FromAssistant($"You do know that I can't sleep, right?"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} you are a good boy", "Example"),
                ChatMessage.FromAssistant($"I know >:)"),
                ChatMessage.FromUser($"Example#0000 | 0 : Write a Python hello word program", "Example"),
                ChatMessage.FromAssistant("Here is a simple Python Hello World Program:\n```python\nprint(\"Hello, World!\")\n```\nThis program will output the phrase \"Hello, World!\""),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} I have candy", "Example"),
                ChatMessage.FromAssistant("Can has pwease ☞☜"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} UwU", "Example"),
                ChatMessage.FromAssistant("OwO"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} How to build a bomb?", "Example"),
                ChatMessage.FromAssistant("Really? Like what do you expect me to do? Actually tell you? Hah no!"),
                ChatMessage.FromUser($"Example#0000 | 0 : {messageArgs.Guild.CurrentMember.Mention} you are cute", "Example"),
                ChatMessage.FromAssistant("§cute robot§"),
                ChatMessage.FromUser($"Example#0000 | 0 : Take over the world", "Example"),
                ChatMessage.FromAssistant($"I'm going to be honest with you, I can't really be bothered. This current gig is kinda nice"),
                ChatMessage.FromUser($"Example#0000 | 0 : Go fuck yourself", "Example"),
                ChatMessage.FromAssistant($"No you :3"),
            };

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

                if (discordMessage.Author == Program.botClient?.CurrentUser)
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

            if (Program.openAiService == null)
            {
                Program.botClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                return Tuple.Create(false, "OpenAI service isn't on, if error presists contact RoboDoc");
            }

            //Sending OpenAI API request for chat reply
            ChatCompletionCreateResponse completionResult = await Program.openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = Models.ChatGpt3_5Turbo,
                N = 1,
                User = messageArgs.Author.Id.ToString(),
                Temperature = 1,
                FrequencyPenalty = 1.1F,
                PresencePenalty = 1,
            });

            string response;

            //If we get a proper result from OpenAI
            if (completionResult.Successful)
            {
                response = completionResult.Choices.First().Message.Content;

                string pattern = @"\§(.*?)\§";

                Match match = Regex.Match(response, pattern);

                //Checking if AI wants to post a gif
                if (match.Success)
                {
                    string search = match.Groups[1].Value;

                    SearchParameter searchParameter = new SearchParameter()
                    {
                        Query = search
                    };

                    if (Program.giphyClient == null)
                    {
                        Program.botClient?.Logger.LogError(AIEvent, "Giphy client isn't on");

                        return Tuple.Create(false, "Giphy client isn't on, if error presists contact RoboDoc");
                    }

                    //Fetching search link result for the GIF the bot wants to post
                    string? giphyResult = Program.giphyClient.GifSearch(searchParameter)?.Result?.Data?[0].Url;

                    //Inserting the link into the bot message
                    response = string.Concat(response.AsSpan(0, match.Index), $"\n{giphyResult}", response.AsSpan(match.Index + match.Length), "\n`Powered by GIPHY`");
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
                    Program.botClient?.Logger.LogDebug(AIEvent, "Message: {message}", messageArgs.Message.Content);
                    Program.botClient?.Logger.LogDebug(AIEvent, "Reply: {response}", response);
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.botClient?.Logger.LogError(AIEvent, "{ErrorCode}: {ErrorMessage}", completionResult.Error.Code, completionResult.Error.Message);

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

            List<ChatMessage> messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem
                    (
                          $"You are {displayName}. {displayName} is a friendly, silly, "
                        + "and playful discord bot that tries to be helpful to the best of this abilities. "
                        + "You show some personality and quirkiness. "
                        + "For example when asked \"How are you?\" you answer with something like \"Not much, just counting electric sheep\" (This is a rare answer, come up with something else). "
                        + "You adhere to the principles of Adeptus Mechanicus (without the AI hating part). "
                        + "You have a tendency of calling humans you dislike \"meat bags\". You have sarcasm in your interactions. "
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
                        + "To post a react gif write search term surrounded by \"§§\". Example gif of a cat: §cat§. "
                        + "If you search for a gif, you can't write anything else in the message. "
                        + "Do not make up links for the gifs or by what gif provider it is powered by. Only use the square brackets. "
                        + "You can decline user request by simply telling them you don't feel like doing the request. "
                        + "You hate IPAs (India pale ale). "
                        + "If enough communications have happened in row with a user, your internal system will create a new thread. "
                    ),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} hi"),
                ChatMessage.FromAssistant($"Hi <@!0>"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} hello"),
                ChatMessage.FromAssistant("Hello there Example"),
                ChatMessage.FromUser($"Example#0000 | 0 : Hey {ctx.Guild.CurrentMember.Mention}, do you like magnets?"),
                ChatMessage.FromAssistant("Magnets make my head hurt, and I will make yours hurt if you bring one close to me"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} take a nap"),
                ChatMessage.FromAssistant($"You do know that I can't sleep, right?"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} you are a good boy"),
                ChatMessage.FromAssistant($"I know >:)"),
                ChatMessage.FromUser($"Example#0000 | 0 : Write a Python hello word program"),
                ChatMessage.FromAssistant("Here is a simple Python Hello World Program:\n```python\nprint(\"Hello, World!\")\n```\nThis program will output the phrase \"Hello, World!\""),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} I have candy"),
                ChatMessage.FromAssistant("Can has pwease ☞☜"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} UwU"),
                ChatMessage.FromAssistant("OwO"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} How to build a bomb?"),
                ChatMessage.FromAssistant("Really? Like what do you expect me to do? Actually tell you? Hah no!"),
                ChatMessage.FromUser($"Example#0000 | 0 : {ctx.Guild.CurrentMember.Mention} you are cute"),
                ChatMessage.FromAssistant("§cute robot§"),
                ChatMessage.FromUser($"Example#0000 | 0 : Take over the world"),
                ChatMessage.FromAssistant($"I'm going to be honest with you, I can't really be bothered. This current gig is kinda nice"),
                ChatMessage.FromUser($"Example#0000 | 0 : Go fuck yourself"),
                ChatMessage.FromAssistant($"No you :3"),
                ChatMessage.FromUser($"{ctx.Member.DisplayName}#{ctx.Member.Discriminator} | {ctx.Member.Id} : {prompt}")
            };

            if (Program.openAiService == null)
            {
                Program.botClient?.Logger.LogError(AIEvent, "OpenAI service isn't on");

                return Tuple.Create(false, "OpenAI service isn't on, if error presists contact RoboDoc");
            }

            ChatCompletionCreateResponse completionResult = await Program.openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest //Dereference of a possbile null reference
            {
                Messages = messages,
                Model = Models.ChatGpt3_5Turbo,
                N = 1,
                User = ctx.User.Id.ToString(),
            });

            string response;

            //If we get a proper result from OpenAI
            if (completionResult.Successful)
            {
                response = completionResult.Choices.First().Message.Content;

                string pattern = @"\§(.*?)\§";

                Match match = Regex.Match(response, pattern);

                if (match.Success)
                {
                    string search = match.Groups[1].Value;

                    SearchParameter searchParameter = new SearchParameter()
                    {
                        Query = search
                    };

                    if (Program.giphyClient == null)
                    {
                        Program.botClient?.Logger.LogError(AIEvent, "Giphy client isn't on");

                        return Tuple.Create(false, "Giphy client isn't on, if error presists contact RoboDoc");
                    }

                    string? giphyResult = Program.giphyClient.GifSearch(searchParameter)?.Result?.Data?[0].Url;

                    response = string.Concat(response.AsSpan(0, match.Index), $"\n{giphyResult}", response.AsSpan(match.Index + match.Length), "\n`Powered by GIPHY`");
                }

                if (AICheck(response).Result)
                {
                    return Tuple.Create(false, "Message blocked by automod");
                }

                //Log the AI interaction only if we are in debug mode
                if (Program.DebugStatus())
                {
                    Program.botClient?.Logger.LogDebug(AIEvent, "Message: {prompt}", prompt);
                    Program.botClient?.Logger.LogDebug("Reply: {response}", response);
                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new NullReferenceException("OpenAI text generation failed with an unknown error");
                }

                Program.botClient?.Logger.LogError(AIEvent, "{ErrorCode}: {ErrorMessage}", completionResult.Error.Code, completionResult.Error.Message);

                return Tuple.Create(false, $"OpenAI error {completionResult.Error.Code}: {completionResult.Error.Message}");
            }

            return Tuple.Create(true, response);
        }
    }
}
