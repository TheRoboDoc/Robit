using DSharpPlus.EventArgs;
using static Robit.FileManager;

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
            List<ResponseManager.ResponseEntry> responseEntries = ResponseManager.ReadEntries(messageArgs.Guild.Id.ToString());

            string messageLower = messageArgs.Message.Content.ToLower();

            Tuple<bool, string> response = Tuple.Create(false, "No saved matches found"); ;

            await Task.Run(() =>
            {
                foreach (ResponseManager.ResponseEntry responseEntry in responseEntries)
                {
                if (Regex.IsMatch(messageLower, $@"\b{Regex.Escape(responseEntry.content)}"))
                    {
                        response = Tuple.Create(true, responseEntry.response);
                        break;
                    }
                }
            });

            return response;
        }
    }
}
