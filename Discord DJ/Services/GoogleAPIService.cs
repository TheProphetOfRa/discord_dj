using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord_DJ.Services
{
    public class GoogleAPIService
    {
        private YouTubeService m_youtubeService;

        public GoogleAPIService()
        {
            m_youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Config.YoutubeAPIKey,
                ApplicationName = this.GetType().ToString()
            });
        }

        public async Task<string> TryGetVideoUrlForSearchTerms(string searchTerms)
        {
            SearchResource.ListRequest searchRequest = m_youtubeService.Search.List("snippet");
            searchRequest.Q = searchTerms;
            searchRequest.MaxResults = 5;
            var searchResponse = await searchRequest.ExecuteAsync();

            foreach (var result in searchResponse.Items)
            {
                if (result.Id.Kind == "youtube#video")
                {
                    return "https://www.youtube.com/watch?v=" + result.Id.VideoId;
                }
            }

            return null;
        }
    }
}
