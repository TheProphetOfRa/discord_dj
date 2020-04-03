using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ytdl_cs;

namespace Discord_DJ.Services
{
    public class YoutubeDLService
    {
        private readonly IServiceProvider _services;

        Ytdl _youtubeDownloader;

        public YoutubeDLService(IServiceProvider services)
        {
            _services = services;
            _youtubeDownloader = new Ytdl();
        }

        public async Task<Model.MusicPlayer.VideoInfo> GetInfoForVideoUrl(string videoUrl)
        {
            Uri videoUri = new Uri(videoUrl);
            NameValueCollection query = HttpUtility.ParseQueryString(videoUri.Query);
            string videoId = "";

            if (query.AllKeys.Contains("v"))
            {
                videoId = query["v"];
            }
            else
            {
                videoId = videoUri.Segments.Last();
            }

            VideoInfo info = await _youtubeDownloader.GetVideoInfoAsync(videoId, TimeSpan.FromSeconds(3));

            if (info == null)
            {
                return null;
            }

            return new Model.MusicPlayer.VideoInfo
            {
                title = info.Title,
                url = videoUrl
            };
        }
    }
}
