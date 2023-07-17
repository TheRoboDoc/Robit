using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using static Robit.FileManager.YouTubeNotificationManager;

namespace Robit.YouTube
{
    public class NotificationManager
    {
        public readonly YouTubeService YouTubeService;

        public readonly string GuildID;

        private NotificationManager(string guildID)
        {
            YouTubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = Tokens.YoutubeToken,
                ApplicationName = "Robit"
            });

            GuildID = guildID;
        }

        public static NotificationManager Create(string guildID)
        {
            return new NotificationManager(guildID);
        }
        
        public async Task AddNotification(string url, string notificationText, bool AIGenerated)
        {
            NotificationEntry notificationEntry = new NotificationEntry()
            {
                ChannelID = ExtractChannelID(url),
                NotificationText = notificationText,
                UseAI = AIGenerated
            };

            await WriteEntry(notificationEntry, GuildID);
        }

        public async Task RemoveNotification(string url)
        {
            string channelID = ExtractChannelID(url);

            await RemoveEntry(channelID, GuildID);
        }

        private async Task CheckChannelActivities()
        {
            NotificationEntry[]? notificationEntries = await ReadEntries(GuildID);

            if (notificationEntries == null) { return; }

            foreach (NotificationEntry entry in notificationEntries)
            {
                await CheckChannelActivity(entry.ChannelID);
            }
        }

        private async Task CheckChannelActivity(string channelID)
        {
            try
            {
                ChannelsResource.ListRequest channelListRequest = YouTubeService.Channels.List("contentDetails");

                channelListRequest.Id = channelID;

                ChannelListResponse channelListResponse = await channelListRequest.ExecuteAsync();

                Channel channel = channelListResponse.Items[0];

                string uploadsPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;

                PlaylistItemsResource.ListRequest playlistItemsListRequest = YouTubeService.PlaylistItems.List("snippet");

                playlistItemsListRequest.PlaylistId = uploadsPlaylistId;

                playlistItemsListRequest.MaxResults = 1;

                PlaylistItemListResponse playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                if (playlistItemsListResponse.Items.Count > 0)
                {
                    PlaylistItem latestVideo = playlistItemsListResponse.Items[0];

                    string videoTitle = latestVideo.Snippet.Title;
                    string videoId = latestVideo.Snippet.ResourceId.VideoId;
                    string videoDescription = latestVideo.Snippet.Description;

                    await HandleReply(videoTitle, videoId, videoDescription);
                }
            }
            catch (Exception ex)
            {
                Program.BotClient?.Logger.LogWarning("$An error occurred while checking channel activity: {message}", ex.Message);
            }
        }

        private Task<string> HandleReply(string videoTitle, string videoId, string videoDescription)
        {
            throw new NotImplementedException();
        }

        public static string ExtractChannelID(string channelURL)
        {
            if (channelURL.Contains("/channel/"))
            {
                Uri uri = new Uri(channelURL);

                string[] segments = uri.Segments;

                string channelID = segments[^1].TrimEnd('/');

                return channelID;
            }
            else if (channelURL.Contains('@'))
            {
                string[] segments = channelURL.Split('/');

                string channelID = segments[^1];

                return channelID;
            }
            else
            {
                throw new ArgumentException("Invalid YouTube channel URL");
            }
        }
    }
}
