using Discord;
using Discord_DJ.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_DJ.Services
{
    public enum TriviaServiceResult
    {
        AlreadyRunningQuiz,
        Success
    };

    public struct TriviaItem
    {
        public string url;
        public List<string> singer;
        public List<string> title;
    }

    public class TriviaService
    {
        private readonly IServiceProvider _services;

        private Dictionary<ulong, TriviaQuiz> _mapGuildIdsToQuizes = new Dictionary<ulong, TriviaQuiz>();

        List<TriviaItem> _triviaItems = new List<TriviaItem>();

        public TriviaService(IServiceProvider services)
        {
            _services = services;
            _triviaItems = JsonConvert.DeserializeObject<List<TriviaItem>>(File.ReadAllText("Resources/Music/trivia.json"));
        }

        public async Task<TriviaServiceResult> StartQuizForGuild(ulong guildId, IVoiceChannel channel, int numQuestions)
        {
            if (_mapGuildIdsToQuizes.ContainsKey(guildId))
            {
                return TriviaServiceResult.AlreadyRunningQuiz;
            }

            _triviaItems.Shuffle();

            List<TriviaItem> itemsForQuiz = _triviaItems.Take(5).ToList();

            TriviaQuiz quiz = new TriviaQuiz(guildId, channel, itemsForQuiz);
            await quiz.StartQuiz();
            return TriviaServiceResult.Success;
        }
    }
}
