using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ytdl_cs;

namespace Discord_DJ
{
    public class VideoInfo
    {
        private class FormatComparer : IComparer<Format>
        {
            private static readonly Dictionary<String, int> kMapFormatToWeight = new Dictionary<String, int>
            {
                {"large", 4},
                {"medium", 3},
                {"small", 2},
                {"tiny", 1}
            };

            public int Compare(Format a, Format b)
            {
                if (a.Quality == b.Quality)
                {
                    return 0;
                }

                if (kMapFormatToWeight[a.Quality] > kMapFormatToWeight[b.Quality])
                {
                    return 1;
                }

                return -1;
            }
        };


        private String m_videoId;

        private ytdl_cs.VideoInfo m_youtubeInfo;

        private Format m_bestAudioFormat;
        public Format BestAudioFormat
        {
            get
            {
                return m_bestAudioFormat;
            }
        }

        public VideoInfo(string i_videoId)
        {
            m_videoId = i_videoId;
        }

        public async Task GetVideoInfo()
        {
            Ytdl youtubeDownloader = new Ytdl();

            m_youtubeInfo = await youtubeDownloader.GetVideoInfoAsync(m_videoId, TimeSpan.FromSeconds(3));

            List<Format> audioFormats = new List<Format>();

            foreach (Format f in m_youtubeInfo.Formats)
            {
                if (f.Type.StartsWith("audio"))
                {
                    audioFormats.Add(f);
                }
            }

            if (audioFormats.Count > 0)
            {
                audioFormats.Sort(new FormatComparer());

                m_bestAudioFormat = audioFormats[0];
            }
            else
            {
                m_youtubeInfo.Formats.Sort(new FormatComparer());
                m_bestAudioFormat = m_youtubeInfo.Formats[0];
            }
        }
    }
}
