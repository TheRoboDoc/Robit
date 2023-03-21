using Robit.Converter;
using System.Net;
using System.Text.Json;

namespace Robit
{
    /// <summary>
    /// Class responsible for managment of files
    /// </summary>
    public static class FileManager
    {
        /// <summary>
        /// Paths to directories that the bot uses to store different kinds of data
        /// </summary>
        public readonly struct Paths
        {
            public static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
            public static readonly string dataPath = $@"{basePath}/ResponseData";
            public static readonly string tempMediaPath = $@"{basePath}TempMedia";
            public static readonly string resources = $@"{basePath}/Resources";
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

                foreach (var field in typeof(Paths).GetFields())
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
                WebClient client = new WebClient(); //Needs to be replaced with HttpClient at somepoint

                string path = IDToPath(channelID);

                DirectoryInfo directory = new DirectoryInfo(path);

                await Task.Run(() =>
                {
                    if (!directory.Exists)
                    {
                        directory.Create();
                    }

                    client.DownloadFile(new Uri(url), $"{path}/download.{format}");

                    Task.Delay(2000); //Things don't work properly if this is removed
                });
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
                List<ResponseEntry> responseEntries = new List<ResponseEntry>();

                ResponseEntry responseEntryToModify = new ResponseEntry();

                string path = IDToPath(guildID);

                responseEntries = ReadEntries(guildID);

                bool found = false;

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

                    responseEntries.Remove(responseEntryToModify);
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
                List<ResponseEntry> responseEntries;

                ResponseEntry responseEntryToRemove = new ResponseEntry();

                responseEntries = ReadEntries(guildID);

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

                JsonSerializer.Serialize(fileStream, responseEntries, jsonSerializerOptions);

                fileStream.Close();
            }

            /// <summary>
            /// Reads a <c>ResponseEntry</c> JSON that corresponds to a given guildID
            /// </summary>
            /// <param name="guildID">ID of the guild</param>
            /// <returns><c>ResponseEntry</c> list</returns>
            public static List<ResponseEntry> ReadEntries(string guildID)
            {
                List<ResponseEntry>? responseEntries = new List<ResponseEntry>();

                string path = IDToPath(guildID);

                if (!FileExists(path))
                {
                    CreateFile(path);
                }

                string jsonString = File.ReadAllText(path);

                if (!string.IsNullOrEmpty(jsonString))
                {
                    responseEntries = JsonSerializer.Deserialize<List<ResponseEntry>>(jsonString);
                }
                else
                {
                    throw new NullReferenceException("List of response entries is null");
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
                List<ResponseEntry> responseEntries;

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

                responseEntries.Add(responseEntry);

                FileStream fileStream = File.OpenWrite(path);

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                JsonSerializer.Serialize(fileStream, responseEntries, jsonSerializerOptions);

                fileStream.Close();
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
            private static bool FileExists(string fileDir)
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
            private static void CreateFile(string fileDir)
            {
                FileInfo fileInfo = new FileInfo(fileDir);

                fileInfo.Create().Dispose();
            }
        }
    }
}
