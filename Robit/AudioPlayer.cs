using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Robit
{
    /// <summary>
    ///     Audio player for handling audio playing for each individual guild
    /// </summary>
    public class AudioPlayer
    {
        /// <summary>
        ///     Channel where audio is playing
        /// </summary>
        public DiscordChannel Channel { private set; get; }

        /// <summary>
        ///     Channel play was ordered from
        /// </summary>
        public DiscordChannel TextChannel { private set; get; }

        /// <summary>
        ///     Folder with the audio files
        /// </summary>
        public string FolderPath { private set; get; }

        /// <summary>
        ///     Current playing song
        /// </summary>
        public string? CurrentSong { private set; get; } = null;

        private readonly bool Loop;

        private bool Skipping = false;

        private VoiceNextConnection? connection;

        private List<FileInfo> audios = new();

        private Stream? pcm;

        private Process? ffmpeg;

        private VoiceTransmitSink? transmit;

        public AudioPlayer(string folderPath, DiscordChannel channel, DiscordChannel textChannel, bool loop = true)
        {
            FolderPath = folderPath;

            Loop = loop;

            DirectoryInfo dir = new(FolderPath);

            foreach (FileInfo file in dir.GetFiles())
            {
                Program.BotClient?.Logger.LogDebug("Song: {songname}", file.Name);
                audios.Add(file);
            }

            Program.BotClient?.Logger.LogDebug("Setting channel");

            Channel = channel;

            Program.BotClient?.Logger.LogDebug("Setting text channel");

            TextChannel = textChannel;
        }

        /// <summary>
        ///     Play music
        /// </summary>
        /// <exception cref="NullReferenceException">Failed to start ffmpeg</exception>
        public async Task Play()
        {
            Random rnd = new();

            Program.BotClient?.Logger.LogDebug("Connecting to voice channel");

            _ = Task.Run(async () =>
            {
                connection = await Channel.ConnectAsync();
            });

            await Task.Delay(3000);

            Program.BotClient?.Logger.LogDebug("Connected to voice channel");

            while (true)
            {
                Skipping = false;

                Program.BotClient?.Logger.LogDebug("Entered the loop");

                Program.BotClient?.Logger.LogDebug("Getting ready to pick a song");

                FileInfo audio = audios[rnd.Next(0, audios.Count)];

                Program.BotClient?.Logger.LogDebug("Picked {song}", audio.Name);

                string pattern = @"Fallout New Vegas Radio - |.webm";
                string replacement = "";

                CurrentSong = Regex.Replace(audio.Name, pattern, replacement);

                TextChannel?.SendMessageAsync($"Now playing `{CurrentSong}` in {Channel.Mention}");

                ProcessStartInfo ffmpegStartInfo = new()
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{audio.FullName}"" -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                ffmpeg = Process.Start(ffmpegStartInfo) ?? throw new NullReferenceException("Failed to start ffmpeg");

                Program.BotClient?.Logger.LogDebug("Started ffmpeg");

                pcm = ffmpeg.StandardOutput.BaseStream;

                Program.BotClient?.Logger.LogDebug("Created stream");

                transmit = connection.GetTransmitSink();

                _ = Task.Run(async () =>
                {
                    await pcm.CopyToAsync(transmit);

                    Skipping = true;
                });

                while (!Skipping)
                {
                    //Do nothing;
                }

                await pcm.DisposeAsync();

                Program.BotClient?.Logger.LogDebug("Stopped playing");

                if (Loop)
                {
                    continue;
                }

                audios.Remove(audio);

                Program.BotClient?.Logger.LogDebug("Removed song");

                if (!audios.Any())
                {
                    await pcm.DisposeAsync();

                    ffmpeg.Kill();

                    Program.BotClient?.Logger.LogDebug("Stopped playing");

                    break;
                }
            }

            Program.BotClient?.Logger.LogDebug("The loop has been exited");
        }

        /// <summary>
        ///     Skip a song
        /// </summary>
        public async Task Skip()
        {
            await Task.Run(() =>
            {
                Skipping = true;
            });
        }

        /// <summary>
        ///     Disconnect from the channel and stop playback
        /// </summary>
        public async Task Disconnect()
        {
            ffmpeg?.Kill();

            Program.BotClient?.Logger.LogDebug("FFmpeg killed");

            if (pcm != null)
            {
                await pcm.DisposeAsync();
            }

            Program.BotClient?.Logger.LogDebug("PCM disposed");

            transmit?.Dispose();

            Program.BotClient?.Logger.LogDebug("Transmit disposed");

            audios = new();

            _ = Task.Run(() =>
            {
                connection?.Disconnect();
            });

            Program.BotClient?.Logger.LogDebug("Disconnecting");
        }

        private bool paused = false;

        /// <summary>
        ///     Toggle pause
        /// </summary>
        public async Task Pause()
        {
            await Task.Run(() =>
            {
                if (paused)
                {
                    connection?.ResumeAsync();

                    paused = false;

                    return;
                }

                connection?.Pause();
            });
        }
    }
}
