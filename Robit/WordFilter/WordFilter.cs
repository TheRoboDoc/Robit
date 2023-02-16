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

            if(badWords == null)
            {
                return Tuple.Create(false, (string?) null);
            }

            foreach(string badWord in badWords) 
            { 
                if(sentence.Contains(badWord.ToLower()))
                {
                    return Tuple.Create(true, (string?)badWord);
                }
            }

            return Tuple.Create(false, (string?) null);
        }
    }
}
