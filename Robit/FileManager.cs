using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace Robit
{
    /// <summary>
    /// Class responsible for managment of files
    /// </summary>
    public static class FileManager
    {
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
            /// Folder path to the "ResponseData" folder
            /// </summary>
            private static readonly string dataPath = $@"{AppDomain.CurrentDomain.BaseDirectory}/ResponseData";

            /// <summary>
            /// Checks that the data directory for storing the responses exists
            /// </summary>
            /// <returns>
            /// <list type="table">
            /// <item>True: Directory exists</item>
            /// <item>False: Directory doesn't exist</item>
            /// </list>
            /// </returns>
            public static async Task<bool> DirCheck()
            {
                bool dirExists = false;

                await Task.Run(() =>
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dataPath);

                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                        dirExists = false;
                    }
                    else
                    {
                        dirExists = true;
                    }
                });

                return dirExists;
            }

            /// <summary>
            /// Converts a guild ID into that guild's responses json file path
            /// </summary>
            /// <param name="guildID">ID to convert</param>
            /// <returns>Path to that guilds responses json file</returns>
            private static string IDToPath(string guildID)
            {
                return $@"{dataPath}/{guildID}.json";
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
            public static bool ModifyEntry(string entryName, string content, string response, string guildID)
            {
                List<ResponseEntry> responseEntries = new List<ResponseEntry>();

                ResponseEntry responseEntryToModify = new ResponseEntry();

                string path = IDToPath(guildID);

                responseEntries = ReadEntries(guildID);

                bool found = false;

                foreach(ResponseEntry responseEntry in responseEntries)
                {
                    if(responseEntry.reactName.ToLower() == entryName.ToLower())
                    {
                        responseEntryToModify = responseEntry;
                        found = true;
                        break;
                    }
                }

                if(!found) { return false; }

                ResponseEntry modifiedResponseEntry = new ResponseEntry()
                {
                    reactName = entryName,
                    content = content,
                    response = response
                };

                responseEntries.Remove(responseEntryToModify);
                responseEntries.Add(modifiedResponseEntry);

                OverwriteEntries(responseEntries, guildID);

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
