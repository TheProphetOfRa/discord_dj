using Discord;
using Discord.WebSocket;
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
        NotRunningQuiz,
        Success
    };

    public struct TriviaItem
    {
        public string filepath;
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

        public async Task<TriviaServiceResult> StartQuizForGuild(ulong guildId, IVoiceChannel channel, IChannel textChannel, int numQuestions)
        {
            if (_mapGuildIdsToQuizes.ContainsKey(guildId))
            {
                return TriviaServiceResult.AlreadyRunningQuiz;
            }

            _triviaItems.Shuffle();

            List<TriviaItem> itemsForQuiz = _triviaItems.Take(numQuestions).ToList();

            TriviaQuiz quiz = new TriviaQuiz(guildId, channel, textChannel, itemsForQuiz);
            _mapGuildIdsToQuizes.Add(guildId, quiz);
            quiz.OnFinishedQuestions += OnQuizFinished;
            await quiz.StartQuiz();
            return TriviaServiceResult.Success;
        }

        private void OnQuizFinished(TriviaQuiz quiz)
        {
            _mapGuildIdsToQuizes.Remove(quiz.GuildId);
        }

        public bool IsGuildRunningQuizInChannel(ulong guildId, IChannel channel)
        {
            return _mapGuildIdsToQuizes.ContainsKey(guildId) && _mapGuildIdsToQuizes[guildId].TextChannel == channel;
        }

        public async Task<TriviaServiceResult> ProcessAnswer(ulong guildId, SocketUserMessage message)
        {
            if (!_mapGuildIdsToQuizes.ContainsKey(guildId))
            {
                return TriviaServiceResult.NotRunningQuiz;
            }

            _mapGuildIdsToQuizes[guildId].ProcessAnswer(message);
            return TriviaServiceResult.Success;
        }

        public TriviaServiceResult EndQuiz(ulong guildId)
        {
            if (!_mapGuildIdsToQuizes.ContainsKey(guildId))
            {
                return TriviaServiceResult.NotRunningQuiz;
            }

            _mapGuildIdsToQuizes[guildId].EndQuiz();
            return TriviaServiceResult.Success;
        }
    }
}
