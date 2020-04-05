

using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Discord_DJ.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_DJ.Model
{
    public class TriviaQuiz
    {
        public delegate void OnFinishedQuestionsDelegate(TriviaQuiz quiz);

        private ulong _guildId;
        private IVoiceChannel _channel;
        private readonly IChannel _textChannel;
        private List<TriviaItem> _questions;

        public IChannel TextChannel
        {
            get
            {
                return _textChannel;
            }
        }

        private TriviaItem _currentItem;
        private bool _foundSinger = false;
        private bool _foundTitle = false;
        private bool _endingItem = false;

        private Dictionary<ulong, int> _mapPlayersToScores = new Dictionary<ulong, int>();
        private Dictionary<ulong, string> _mapIdsToUsernames = new Dictionary<ulong, string>();

        public event OnFinishedQuestionsDelegate OnFinishedQuestions;

        private IAudioClient _audioConnection = null;
        private CancellationTokenSource _streamCanceller = null;
        private Process _streamProcess = null;
        private Stream _outputStream = null;
        private AudioOutStream _discordStream = null;

        public TriviaQuiz(ulong guildId, IVoiceChannel channel, IChannel textChannel, List<TriviaItem> questions)
        {
            _guildId = guildId;
            _questions = questions;
            _channel = channel;
            _textChannel = textChannel;
        }            

        public async Task<TriviaServiceResult> StartQuiz()
        {
            if (_audioConnection == null || _audioConnection.ConnectionState != ConnectionState.Connected)
            {
                _audioConnection = await _channel.ConnectAsync();
            }

            var channelMembers = await _channel.GetUsersAsync().FlattenAsync();

            foreach (IGuildUser member in channelMembers)
            {
                if (!member.IsBot)
                {
                    _mapPlayersToScores.Add(member.Id, 0);
                    _mapIdsToUsernames.Add(member.Id, member.Username);
                }
            }

            PlaySnippet(_questions[0]);

            return TriviaServiceResult.Success;
        }

        private async Task PlaySnippet(TriviaItem item)
        {
            _questions.RemoveAt(0);
            _currentItem = item;

            if (_streamCanceller != null)
            {
                _streamCanceller.Dispose();
            }

            _streamCanceller = new CancellationTokenSource();
            _streamProcess = CreateStream(item.url);
            _outputStream = _streamProcess.StandardOutput.BaseStream;
            _discordStream = _audioConnection.CreatePCMStream(AudioApplication.Music);

            try
            {
                Task killTask = Task.Run(() => _streamProcess.WaitForExit(30000), _streamCanceller.Token);
                await Task.WhenAny(killTask, _outputStream.CopyToAsync(_discordStream, _streamCanceller.Token));
                await EndSnippet();
            }    
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task ProcessAnswer(SocketUserMessage answer)
        {
            string lowerAnswer = answer.Content.ToLower();
            if (!_mapPlayersToScores.ContainsKey(answer.Author.Id))
            {
                return;
            }

            bool consumed = false;

            if (!_foundSinger)
            {
                foreach (string singer in _currentItem.singer)
                {
                    if (lowerAnswer == singer.ToLower())
                    {
                        consumed = true;
                        _foundSinger = true;
                        _mapPlayersToScores[answer.Author.Id] += 1;
                        break;
                    }
                }
            }

            if (!consumed && !_foundTitle)
            {
                foreach (string title in _currentItem.title)
                {
                    if (lowerAnswer == title.ToLower())
                    {
                        consumed = true;
                        _foundTitle = true;
                        _mapPlayersToScores[answer.Author.Id] += 1;
                        break;
                    }
                }
            }

            if (!consumed && answer.Content.Contains(' ') && (!_foundTitle || !_foundSinger))
            {
                foreach (string title in _currentItem.title)
                {
                    foreach (string singer in _currentItem.singer)
                    {
                        if (lowerAnswer == title.ToLower() + ' ' + singer.ToLower() ||
                            lowerAnswer == singer.ToLower() + ' ' + title.ToLower())
                        {
                            consumed = true;
                            _mapPlayersToScores[answer.Author.Id] += ((_foundSinger || _foundTitle) ? 1 : 2);
                            _foundTitle = true;
                            _foundSinger = true;
                            break;
                        }
                    } 
                    if (consumed)
                    {
                        break;
                    }
                }
            }

            if (consumed)
            {
                await answer.AddReactionAsync(new Emoji("\u2611"));
            }
            else
            {
                await answer.AddReactionAsync(new Emoji("\u274C"));
            }

            if (_foundSinger && _foundTitle)
            {
                await EndSnippet();
            }
        }       

        private async Task ShowQuestionCard(TriviaItem question)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"The song was: {_currentItem.title[0]} - {_currentItem.singer[0]}");
            var items = from pair in _mapPlayersToScores orderby pair.Value descending select pair;
            foreach (KeyValuePair<ulong, int> pair in items)
            {
                builder.AddField(_mapIdsToUsernames[pair.Key], pair.Value, false);
            }
            await (_textChannel as ISocketMessageChannel).SendMessageAsync("", false, builder.Build());
        }

        private async Task ShowFinalLeaderboard()
        {
            var items = from pair in _mapPlayersToScores orderby pair.Value descending select pair;
            EmbedBuilder builder = new EmbedBuilder();
            int i = 0;
            foreach (KeyValuePair<ulong, int> pair in items)
            {
                if (i == 0)
                {
                    builder.WithTitle($"{new Emoji("👑")} The winner is: {_mapIdsToUsernames[pair.Key]}");
                }
                else
                {
                    builder.AddField(_mapIdsToUsernames[pair.Key], pair.Value, false);
                }
                ++i;
            }
            await (_textChannel as ISocketMessageChannel).SendMessageAsync("", false, builder.Build());
        }

        private async Task EndSnippet()
        {
            //Threads are hard
            if (!_endingItem)
            {
                _endingItem = true;                
                _streamCanceller.Cancel();
                _streamProcess.Dispose();
                _outputStream.Dispose();
                _discordStream.Dispose();
                _foundSinger = false;
                _foundTitle = false;
                await ShowQuestionCard(_currentItem);

                if (_questions.Count > 0)
                {
                    _endingItem = false;
                    await PlaySnippet(_questions[0]);
                }
                else
                {
                    _endingItem = false;
                    await ShowFinalLeaderboard();
                    await _channel.DisconnectAsync();
                    OnFinishedQuestions?.Invoke(this);
                }
            }
        }

        private Process CreateStream(string url) //TODO: Move this to it's own class that can return the relevant process based on system
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
