using System.Text.Json;

namespace Robit.WordFilter
{
    /// <summary>
    /// Contains tools for checking out filtered words
    /// </summary>
    public static class WordFilter
    {
        /// <summary>
        /// Checks if the given sentence contains a blacklisted word
        /// </summary>
        /// <param name="sentence">Sentence to check</param>
        /// <returns>
        /// A tuple with a bool and a string. The bool is true if a blacklisted word was detected and false otherwise.
        /// String contains the word that was detected, otherwise the string is null.
        /// </returns>
        public static Tuple<bool, string?> Check(string sentence)
        {
            sentence = sentence.ToLower();

            List<string>? badWords = JsonSerializer.Deserialize<List<string>>(BLACKLIST.blacklist);

            if (badWords == null)
            {
                return Tuple.Create(false, (string?)null);
            }

            //A duplicate of what Response Task does
            string[] wordsInSentence = sentence.Split(' ');

            foreach (string word in wordsInSentence)
            {
                foreach (string badWord in badWords)
                {
                    if (badWord == word.ToLower())
                    {
                        return Tuple.Create(true, (string?)word);
                    }
                }
            }

            return Tuple.Create(false, (string?)null);
        }

        /// <summary>
        /// Removes bunch of special characters from a given string (if any are found)
        /// </summary>
        /// <param name="aString">String to clear of special characters</param>
        /// <returns>A string with removed special characters</returns>
        public static string SpecialCharacterRemoval(string aString)
        {
            //There's a better way, but this works for now
            aString = aString.Replace("+", " ");
            aString = aString.Replace("`", " ");
            aString = aString.Replace("¨", " ");
            aString = aString.Replace("\'", " ");
            aString = aString.Replace(",", " ");
            aString = aString.Replace(".", " ");
            aString = aString.Replace("-", " ");
            aString = aString.Replace("!", "");
            aString = aString.Replace("\"", " ");
            aString = aString.Replace("#", " ");
            aString = aString.Replace("¤", " ");
            aString = aString.Replace("%", " ");
            aString = aString.Replace("&", " ");
            aString = aString.Replace("/", " ");
            aString = aString.Replace("(", " ");
            aString = aString.Replace(")", " ");
            aString = aString.Replace("=", " ");
            aString = aString.Replace("?", " ");
            aString = aString.Replace("´", " ");
            aString = aString.Replace("^", " ");
            aString = aString.Replace("*", " ");
            aString = aString.Replace(";", " ");
            aString = aString.Replace(":", " ");
            aString = aString.Replace("_", " ");
            aString = aString.Replace("§", " ");
            aString = aString.Replace("½", " ");
            aString = aString.Replace("@", " ");
            aString = aString.Replace("£", " ");
            aString = aString.Replace("$", " ");
            aString = aString.Replace("€", " ");
            aString = aString.Replace("{", " ");
            aString = aString.Replace("[", " ");
            aString = aString.Replace("]", " ");
            aString = aString.Replace("}", " ");
            aString = aString.Replace("\\", " ");
            aString = aString.Replace("~", " ");

            return aString;
        }
    }
}
