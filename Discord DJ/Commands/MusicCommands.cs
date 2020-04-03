using Discord;
using Discord.Audio;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

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

            VideoInfo info = new VideoInfo("");

            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "",
                ApplicationName = this.GetType().ToString()
            });

            if (singleVideoDetector.IsMatch(music))
            {
                Console.WriteLine("Sent video link");
                Uri videoUri = new Uri(music);
                NameValueCollection query = HttpUtility.ParseQueryString(videoUri.Query);
                String videoId = "";

                if (query.AllKeys.Contains("v"))
                {
                    videoId = query["v"];
                }
                else
                {
                    videoId = videoUri.Segments.Last();
                }

                Console.WriteLine("Video Id: " + videoId);

                info = new VideoInfo(videoId);
                await info.GetVideoInfo();
                Console.WriteLine("Got video data");
            }
            else
            {
                List<Google.Apis.YouTube.v3.Data.SearchResult> videos = new List<Google.Apis.YouTube.v3.Data.SearchResult>();
                SearchResource.ListRequest searchRequest = ytService.Search.List(music);
                SearchListResponse searchResponse = await searchRequest.ExecuteAsync();
                foreach (Google.Apis.YouTube.v3.Data.SearchResult searchResult in searchResponse.Items)
                {
                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            videos.Add(searchResult);
                            break;
                        default:
                            break;
                    }
                }
            }

            Console.WriteLine("Trying to stream: " + music);

            var audioClient = await channel.ConnectAsync();

            try
            {
                using (var ffplay = CreateStream(music))
                using (var output = ffplay.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        Console.WriteLine("Streaming");
                        await output.CopyToAsync(discord);
                    }
                    finally
                    {
                        Console.WriteLine("Flushing");
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
