using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Robit.Converter;
using System.Reflection;
using System.Text.Json;

namespace Robit
{
    /// <summary>
    /// Class responsible for managment of files
    /// </summary>
    public static class FileManager
    {
        public static readonly EventId FileManagerEvent = new EventId(401, "File Manager");

        /// <summary>
        /// Paths to directories that the bot uses to store different kinds of data
        /// </summary>
        public readonly struct Paths //One day this bot will use an SQL database
        {
            public static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
            public static readonly string dataPath = $@"{basePath}/ResponseData";
            public static readonly string tempMediaPath = $@"{basePath}/TempMedia";
            public static readonly string resources = $@"{basePath}/Resources";
            public static readonly string channelSettings = $@"{basePath}/ChannelSettings";
        }

        /// <summary>
        /// Checks that the directory exists
        /// </summary>
        /// <returns>
        /// A list of all directories created
        /// </returns>
        public static async Task<List<string>> DirCheck()
        {
            List<string> list = new List<string>();

            await Task.Run(() =>
            {
                Paths paths = new Paths();

                foreach (FieldInfo field in typeof(Paths).GetFields())
                {
                    string? path = field.GetValue(paths)?.ToString();

                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    DirectoryInfo directoryInfo = new DirectoryInfo(path);

                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                        list.Add(field.Name);
                    }
                }
            });

            return list;
        }

        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="fileDir">File location</param>
        /// <returns>
        /// <list type="table">
        /// <item><c>True</c>: File exists</item>
        /// <item><c>False</c>: File doesn't exist</item>
        /// </list>
        /// </returns>
        public static bool FileExists(string fileDir)
        {
            FileInfo fileInfo = new FileInfo(fileDir);

            if (!fileInfo.Exists)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a file
        /// </summary>
        /// <param name="fileDir">Location to create the file at</param>
        public static void CreateFile(string fileDir)
        {
            FileInfo fileInfo = new FileInfo(fileDir);

            fileInfo.Create().Dispose();
        }

        /// <summary>
        /// Sets of methods to manage quotes
        /// </summary>
        public static class QuoteManager
        {
            public static readonly EventId QuoteManagerEvent = new EventId(402, "Quote Manager");

            /// <summary>
            /// Quote entry data representation
            /// </summary>
            public struct QuoteEntry
            {
                public string quote { get; set; }
                public string author { get; set; }
                public string bookSource { get; set; }
            }

            private static readonly string path = $"{Paths.resources}/Wh40ImperialQuotes.json";

            /// <summary>
            /// Fetches all of the quote entries
            /// </summary>
            /// <returns>A list of quote entries</returns>
            public static List<QuoteEntry>? FetchAllEntries()
            {
                if (!FileExists(path))
                {
                    CreateFile(path);
                }

                List<QuoteEntry>? quoteEntries = new List<QuoteEntry>();

                string jsonString = File.ReadAllText(path);

                try
                {
                    quoteEntries = JsonConvert.DeserializeObject<List<QuoteEntry>?>(jsonString);
                }
                catch (Exception e)
                {
                    Program.BotClient?.Logger.LogWarning(QuoteManagerEvent, "{Error}", e.Message);
                }

                return quoteEntries;
            }

            /// <summary>
            /// Fetches all the quote entries by matching a given author.
            /// Will call <c>FetchAllEntries()</c> if quote entries are not given as a parameter
            /// </summary>
            /// <param name="author">Author to search by</param>
            /// <param name="quoteEntries">Quote entries to search through</param>
            /// <returns>A list of up to ten quote entries</returns>
            public static List<QuoteEntry>? FetchByAuthor(string author, int count, List<QuoteEntry>? quoteEntries = null)
            {
                if (quoteEntries == null)
                {
                    quoteEntries = FetchAllEntries();
                }
                else if (!quoteEntries.Any())
                {
                    quoteEntries = FetchAllEntries();
                }

                List<QuoteEntry> foundMatches = new List<QuoteEntry>();

                author = author.ToLower();

                int counter = 1;

                quoteEntries ??= new List<QuoteEntry>();

                foreach (QuoteEntry quoteEntry in quoteEntries)
                {
                    if (counter > count)
                    {
                        break;
                    }

                    if (quoteEntry.author.ToLower().Contains(author))
                    {
                        foundMatches.Add(quoteEntry);
                        counter++;
                    }
                }

                return foundMatches;
            }

            /// <summary>
            /// Fetches all the quote entries by matching a given source.
            /// Will call <c>FetchAllEntries()</c> if quote entries are not given as a parameter
            /// </summary>
            /// <param name="source">Author to search by</param>
            /// <param name="quoteEntries">Quote entries to search through</param>
            /// <returns>A list of up to ten quote entries</returns>
            public static List<QuoteEntry>? FetchBySource(string source, int count, List<QuoteEntry>? quoteEntries = null)
            {
                if (quoteEntries == null)
                {
                    quoteEntries = FetchAllEntries();
                }
                else if (!quoteEntries.Any())
                {
                    quoteEntries = FetchAllEntries();
                }

                List<QuoteEntry> foundMatches = new List<QuoteEntry>();

                source = source.ToLower();

                int counter = 1;

                quoteEntries ??= new List<QuoteEntry>();

                foreach (QuoteEntry quoteEntry in quoteEntries)
                {
                    if (counter > count)
                    {
                        break;
                    }

                    if (quoteEntry.bookSource.ToLower().Contains(source))
                    {
                        foundMatches.Add(quoteEntry);
                        counter++;
                    }
                }

                return foundMatches;
            }
        }

        /// <summary>
        /// A set of methods to manage media files
        /// </summary>
        public static class MediaManager
        {
            /// <summary>
            /// Converts a channel ID to a folder path in the TempMedia folder
            /// </summary>
            /// <param name="channelID">Channel ID to convert</param>
            /// <returns>A path to the corresponding channel</returns>
            public static string IDToPath(string channelID)
            {
                return $@"{Paths.tempMediaPath}/{channelID}";
            }

            /// <summary>
            /// Downloads and saves a file from a given link to <i>"Channel ID"</i> folder
            /// </summary>
            /// <param name="url">The link to the file</param>
            /// <param name="channelID">Channel ID</param>
            /// <param name="format">The format of the file</param>
            public static async Task SaveFile(string url, string channelID, string format)
            {
                using HttpClient client = new HttpClient();
                string path = IDToPath(channelID);

                DirectoryInfo directory = new DirectoryInfo(path);

                if (!directory.Exists)
                {
                    directory.Create();
                }

                using HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using Stream contentStream = await response.Content.ReadAsStreamAsync();
                string filePath = Path.Combine(path, $"download.{format}");

                using FileStream fileStream = new FileStream(filePath, FileMode.Create);
                await contentStream.CopyToAsync(fileStream);
            }

            /// <summary>
            /// Converts a downloaded file from the <i>"Channel ID"</i> folder from one format to another
            /// </summary>
            /// <param name="channelID">Channel ID</param>
            /// <param name="formatFrom">Original format of the file</param>
            /// <param name="formatTo">The desired format of the file</param>
            public static async Task Convert(string channelID, string formatFrom, string formatTo)
            {
                string path = IDToPath(channelID);

                await Task.Run(() =>
                {
                    FfmpegConvert.Convert(path, formatFrom, formatTo);
                });
            }

            /// <summary>
            /// Clears the <i>"Channel ID"</i> folder
            /// </summary>
            /// <param name="channelID">Channel ID</param>
            public static async Task ClearChannelTempFolder(string channelID)
            {
                string path = IDToPath(channelID);

                await Task.Run(() =>
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);

                    if (!directoryInfo.Exists)
                    {
                        return;
                    }

                    directoryInfo.Delete(true);
                });
            }
        }

        /// <summary>
        /// Class for managment of response files
        /// </summary>
        public static class ResponseManager
        {
            /// <summary>
            /// Constrcut for the response entry. Contains the name of the response entry, 
            /// content it should be used on, and the response to that content
            /// </summary>
            public struct ResponseEntry
            {
                public string reactName { get; set; }
                public string content { get; set; }
                public string response { get; set; }
            }

            /// <summary>
            /// Converts a guild ID into that guild's responses json file path
            /// </summary>
            /// <param name="guildID">ID to convert</param>
            /// <returns>Path to that guilds responses json file</returns>
            private static string IDToPath(string guildID)
            {
                return $@"{Paths.dataPath}/{guildID}.json";
            }

            /// <summary>
            /// Modifies an response entry
            /// </summary>
            /// <param name="entryName">Name of the entry to modify</param>
            /// <param name="content">By what the response entry should be triggered</param>
            /// <param name="response">Response to the trigger</param>
            /// <param name="guildID">The ID of the guild that response entry is binded to</param>
            /// <returns>
            /// <list type="table">
            /// <item>True: Modification succeeded</item>
            /// <item>False: Modification failed</item>
            /// </list>
            /// </returns>
            public static async Task<bool> ModifyEntry(string entryName, string content, string response, string guildID)
            {
                List<ResponseEntry>? responseEntries = new List<ResponseEntry>();

                ResponseEntry responseEntryToModify = new ResponseEntry();

                string path = IDToPath(guildID);

                responseEntries = ReadEntries(guildID);

                bool found = false;

                if (responseEntries == null)
                {
                    return false;
                }
                else if (!responseEntries.Any())
                {
                    return false;
                }

                await Task.Run(() =>
                {
                    foreach (ResponseEntry responseEntry in responseEntries)
                    {
                        if (responseEntry.reactName.ToLower() == entryName.ToLower())
                        {
                            responseEntryToModify = responseEntry;
                            found = true;
                            break;
                        }
                    }
                });

                if (!found) { return false; }

                await Task.Run(() =>
                {
                    ResponseEntry modifiedResponseEntry = new ResponseEntry()
                    {
                        reactName = entryName,
                        content = content,
                        response = response
                    };

                    responseEntries.Remove(responseEntryToModify); //Dereference of a possbile null reference
                    responseEntries.Add(modifiedResponseEntry);

                    OverwriteEntries(responseEntries, guildID);
                });


                return true;
            }

            /// <summary>
            /// Removes an <c>ResponseEntry</c> from the corresponding JSON file
            /// </summary>
            /// <param name="entryName">Name of the <c>ResponseEntry</c></param>
            /// <param name="guildID">The ID of the guild</param>
            /// <returns>
            /// <list type="table">
            /// <item>True: Removal succeeded</item>
            /// <item>False: Removal failed</item>
            /// </list>
            /// </returns>
            public static bool RemoveEntry(string entryName, string guildID)
            {
                List<ResponseEntry>? responseEntries;

                ResponseEntry responseEntryToRemove = new ResponseEntry();

                responseEntries = ReadEntries(guildID);

                if (responseEntries == null) { return false; }
                else if (!responseEntries.Any()) { return false; }

                foreach (ResponseEntry responseEntry in responseEntries)
                {
                    if (responseEntry.reactName.ToLower() == entryName.ToLower())
                    {
                        responseEntryToRemove = responseEntry;
                        break;
                    }
                }

                if (!responseEntries.Remove(responseEntryToRemove))
                {
                    return false;
                }

                OverwriteEntries(responseEntries, guildID);

                return true;
            }

            /// <summary>
            /// Overwrites saved entry JSON list with a new <c>ResponseEntry</c> list
            /// </summary>
            /// <param name="responseEntries">List to overwrite with</param>
            /// <param name="guildID">The ID of the guild</param>
            public static void OverwriteEntries(List<ResponseEntry> responseEntries, string guildID)
            {
                string path = IDToPath(guildID);

                FileInfo fileInfo = new FileInfo(path);

                fileInfo.Delete();

                FileStream fileStream = File.OpenWrite(path);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                System.Text.Json.JsonSerializer.Serialize(fileStream, responseEntries, jsonSerializerOptions);

                fileStream.Close();
            }

            /// <summary>
            /// Reads a <c>ResponseEntry</c> JSON that corresponds to a given guildID
            /// </summary>
            /// <param name="guildID">ID of the guild</param>
            /// <returns><c>ResponseEntry</c> list</returns>
            public static List<ResponseEntry>? ReadEntries(string guildID)
            {
                string path = IDToPath(guildID);

                if (!FileExists(path))
                {
                    CreateFile(path);
                }

                string jsonString = File.ReadAllText(path);

                List<ResponseEntry>? responseEntries = new List<ResponseEntry>();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    responseEntries = System.Text.Json.JsonSerializer.Deserialize<List<ResponseEntry>>(jsonString);
                }

                return responseEntries;
            }

            /// <summary>
            /// Adds an <c>ResponseEntr0y</c> to a guild's <c>ResponseEntry</c> list
            /// </summary>
            /// <param name="responseEntry"><c>ResponseEntry to add</c></param>
            /// <param name="guildID">ID of the guild</param>
            public static void WriteEntry(ResponseEntry responseEntry, string guildID)
            {
                List<ResponseEntry>? responseEntries;

                string path = IDToPath(guildID);

                FileInfo fileInfo = new FileInfo(path);

                try
                {
                    responseEntries = ReadEntries(guildID);
                    fileInfo.Delete();
                }
                catch
                {
                    responseEntries = new List<ResponseEntry>();
                }

                if (responseEntries == null)
                {
                    return;
                }

                responseEntries.Add(responseEntry);

                FileStream fileStream = File.OpenWrite(path);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                System.Text.Json.JsonSerializer.Serialize(fileStream, responseEntries, jsonSerializerOptions);

                fileStream.Close();
            }
        }

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>
        /// <list type="table">
        /// <item>True: Directory exists</item>
        /// <item>False: Directory doesn't exists</item>
        /// </list>
        /// </returns>
        public static bool DirectoryExists(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            if (!directoryInfo.Exists)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="path">Path of the directory to create</param>
        public static void CreateDirectory(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            directoryInfo.Create();
        }

        /// <summary>
        /// Sets of methods to manage channel settings
        /// </summary>
        public static class ChannelManager
        {
            /// <summary>
            /// Channel specific settings
            /// </summary>
            public struct Channel
            {
                public bool AIIgnore { get; set; }
                public bool autoResponse { get; set; }
            }

            /// <summary>
            /// Converts given guildID and channelID to guild folder path and channel settings json path
            /// </summary>
            /// <param name="guildID">ID of the guild</param>
            /// <param name="channelID">ID of the channel</param>
            /// <returns>
            /// A tuple that contains two strings
            /// <list type="table">
            /// <item>Item 1: A path to the guild directory</item>
            /// <item>Item 2: A path to the channel json file</item>
            /// </list>
            /// </returns>
            private static Tuple<string, string> IDToPath(string guildID, string channelID)
            {
                string guildPath = $"{Paths.channelSettings}/{guildID}";
                string channelPath = $"{guildPath}/{channelID}.json";

                return Tuple.Create(guildPath, channelPath);
            }

            /// <summary>
            /// Reads the channel settings information
            /// </summary>
            /// <param name="guildID">ID of the guild the channel is in</param>
            /// <param name="channelID">ID of the channel</param>
            /// <returns>
            /// A channel struct
            /// </returns>
            public static Channel ReadChannelInfo(string guildID, string channelID)
            {
                Tuple<string, string> paths = IDToPath(guildID, channelID);

                string guildPath = paths.Item1;
                string channelPath = paths.Item2;

                if (!DirectoryExists(guildPath))
                {
                    CreateDirectory(guildPath);
                }

                if (!FileExists(channelPath))
                {
                    CreateFile(channelPath);
                }

                string jsonString = File.ReadAllText(channelPath);

                if (string.IsNullOrEmpty(jsonString))
                {
                    Channel newChannel = new Channel()
                    {
                        AIIgnore = false,
                        autoResponse = true
                    };

                    WriteChannelInfo(newChannel, guildID, channelID, true);

                    return newChannel;
                }

                return System.Text.Json.JsonSerializer.Deserialize<Channel>(jsonString);
            }

            /// <summary>
            /// Writes channel settings information
            /// </summary>
            /// <param name="channel">Channel struct containing the settings you want to write</param>
            /// <param name="guildID">The ID of the guild the channel belongs to</param>
            /// <param name="channelID">The ID of the channel</param>
            /// <param name="overwrite">To overwrite the existing settings(if they exist) or not (default is false)</param>
            /// <exception cref="Exception">If channel settings exist, but overwrite is set to false</exception>
            public static void WriteChannelInfo(Channel channel, string guildID, string channelID, bool overwrite = false)
            {
                Tuple<string, string> paths = IDToPath(guildID, channelID);

                string guildPath = paths.Item1;
                string channelPath = paths.Item2;

                if (!DirectoryExists(guildPath))
                {
                    CreateDirectory(guildPath);
                }

                if (FileExists(channelPath))
                {
                    if (overwrite)
                    {
                        FileInfo fileInfo = new FileInfo(channelPath);

                        fileInfo.Delete();
                        fileInfo.Create().Dispose();
                    }
                    else
                    {
                        throw new Exception("Channel settings entry already exists");
                    }
                }

                FileStream fileStream = File.OpenWrite(channelPath);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                System.Text.Json.JsonSerializer.Serialize(fileStream, channel, jsonSerializerOptions);

                fileStream.Close();
            }
        }
    }
}
