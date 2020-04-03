using Discord;
using Discord.Audio;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord_DJ.Commands
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play some music from youtube")]
        public async Task PlayAsync([Remainder] [Summary("The link or search term")] string music = "")
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("Please join a voice channel and try again.");
                return;
            }

            if (music == "")
            {
                await ReplyAsync("What would you like to play? You can send me a direct link or search like you would in the youtube search bar!");
                return;
            }

            Regex singleVideoDetector = new Regex(@"(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+");

            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Config.YoutubeAPIKey,
                ApplicationName = this.GetType().ToString()
            });

            string videoUrl = null;

            if (!singleVideoDetector.IsMatch(music))
            {
                List<Google.Apis.YouTube.v3.Data.SearchResult> videos = new List<Google.Apis.YouTube.v3.Data.SearchResult>();
                SearchResource.ListRequest searchRequest = ytService.Search.List("snippet");
                searchRequest.Q = music;
                searchRequest.MaxResults = 5;
                SearchListResponse searchResponse = await searchRequest.ExecuteAsync();

                foreach (var result in searchResponse.Items)
                {
                    if (result.Id.Kind == "youtube#video")
                    {
                        videoUrl = "https://www.youtube.com/watch?v=" + result.Id.VideoId;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(videoUrl))
                {
                    await ReplyAsync("Could not find any videos matching those terms. Please try again");
                }
            }
            else
            {
                videoUrl = music;
            }

            var audioClient = await channel.ConnectAsync();

            try
            {
                using (var ffplay = CreateStream(videoUrl))
                using (var output = ffplay.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        await output.CopyToAsync(discord);
                    }
                    finally
                    {
                        await discord.FlushAsync();
                        await channel.DisconnectAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private Process CreateStream(string url)
        {
            string args = $"/C youtube-dl -o - {url} | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1";
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
        }
    }
}
