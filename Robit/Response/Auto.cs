using DSharpPlus.EventArgs;
using System.Text.RegularExpressions;
using static Robit.FileManager;
using static Robit.FileManager.ResponseManager;

namespace Robit.Response
{
    /// <summary>
    /// Handles autoresponses
    /// </summary>
    public static class Auto
    {
        /// <summary>
        /// Checks for saved auto responses and generates an appropriate response
        /// </summary>
        /// <param name="messageArgs">Discord message creation arguments</param>
        /// <returns>
        /// <list type="table">
        /// <listheader>A tuple containing response information</listheader>
        /// <item>
        /// <list type="table">
        /// <listheader>Item1 (bool)</listheader>
        /// <item>True: Generation succeeded</item>
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
        public static async Task<Tuple<bool, string>> GenerateAutoResponse(MessageCreateEventArgs messageArgs)
        {
            //Fetching entries for the server
            List<ResponseEntry>? responseEntries = ReadEntries(messageArgs.Guild.Id.ToString());

            string messageLower = messageArgs.Message.Content.ToLower();

            Tuple<bool, string> response = Tuple.Create(false, "No saved matches found");

            if (responseEntries == null)
            {
                return response;
            }
            else if (!responseEntries.Any())
            {
                return response;
            }

            //Checking if message contains the response trigger
            await Task.Run(() =>
            {
                foreach (ResponseEntry responseEntry in responseEntries)
                {
                    if (Regex.IsMatch(messageLower, $@"\b{Regex.Escape(responseEntry.content)}(?!\w)"))
                    {
                        response = Tuple.Create(true, responseEntry.response);
                        break;
                    }
                }
            });

            return response;
        }

        public static async Task<Tuple<bool, string>> GenerateAutoReact(MessageCreateEventArgs messageArgs)
        {
            List<EmoteReactManager.EmoteReactEntry>? reactEntries = EmoteReactManager.ReadEntries(messageArgs.Guild.Id.ToString());

            string messageLower = messageArgs.Message.Content.ToLower();

            Tuple<bool, string> response = Tuple.Create(false, "No saved matches found");

            if (reactEntries == null)
            {
                return response;
            }
            else if (!reactEntries.Any())
            {
                return response;
            }

            await Task.Run(() =>
            {
                foreach (EmoteReactManager.EmoteReactEntry reactEntry in reactEntries)
                {
                    if (Regex.IsMatch(messageLower, $@"\b{Regex.Escape(reactEntry.Trigger)}(?!\w)"))
                    {
                        response = Tuple.Create(true, reactEntry.DiscordEmoji);
                    }
                }
            });

            return response;
        }
    }
}
