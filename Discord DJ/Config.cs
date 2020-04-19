using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Discord_DJ
{
    public static class Config
    {

        private static string m_botAPIKey;
        private static string m_youtubeAPIKey;

        public static string BotAPIKey
        {
            get
            {
                return m_botAPIKey;
            }
        }

        public static string YoutubeAPIKey
        {
            get
            {
                return m_youtubeAPIKey;
            }
        }

        public static void LoadConfig()
        {
            var configString = File.ReadAllText("config.json");
            var configJson = JObject.Parse(configString);

            JToken botToken;
            JToken youtubeToken;

            if (configJson.TryGetValue("botAPIKey", out botToken))
            {
                m_botAPIKey = botToken.Value<string>();
            }
            else
            {
                throw new Exception("Failed to load config file");
            }

            if (configJson.TryGetValue("youtubeAPIKey", out youtubeToken))
            {
                m_youtubeAPIKey = youtubeToken.Value<string>();
            }
            else
            {
                throw new Exception("Failed to load config file");
            }
        }
    }
}
