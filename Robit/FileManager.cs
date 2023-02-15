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

                foreach(var field in typeof(Paths).GetFields())
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

        public static class MediaManager
        {
            public static string IDToPath(string channelID)
            {
                return $@"{Paths.tempMediaPath}/{channelID}";
            }

            public static async Task SaveFile(string url, string channelID , string format)
            {
                WebClient client = new WebClient();

                string path = IDToPath(channelID);

                DirectoryInfo directory = new DirectoryInfo(path);

                await Task.Run(() =>
                {
                    if(!directory.Exists)
                    {
                        directory.Create();
                    }

                    client.DownloadFile(new Uri(url), $"{path}/download.{format}");

                    Thread.Sleep(2000);
                });

                
            }

            public static async Task Convert(string channelID, string formatFrom, string formatTo)
            {
                string path = IDToPath(channelID);

                await Task.Run(() =>
                {
                    FfmpegConvert.Convert(path, formatFrom, formatTo);
                });
            }

            public static async Task ClearChannelTempFolder(string channelID)
            {
                string path = IDToPath(channelID);

                await Task.Run(() =>
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);

                    List<FileInfo> files = directoryInfo.GetFiles().ToList();

                    foreach (FileInfo file in files)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch 
                        {
                            
                        }
                    }
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

                foreach(ResponseEntry responseEntry in responseEntries)
                {
                    if(responseEntry.reactName.ToLower() == entryName.ToLower())
                    {
                        responseEntryToRemove = responseEntry;
                        break;
                    }
                }

                if(!responseEntries.Remove(responseEntryToRemove))
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
                List<ResponseEntry> responseEntries = new List<ResponseEntry>();

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

                return responseEntries;
            }

            /// <summary>
            /// Adds an <c>ResponseEntry</c> to a guild's <c>ResponseEntry</c> list
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
            /// <item>True: File exists</item>
            /// <item>False: File doesn't exist</item>
            /// </list>
            /// </returns>
            private static bool FileExists(string fileDir)
            {
                FileInfo fileInfo = new FileInfo(fileDir);

                if(!fileInfo.Exists)
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
