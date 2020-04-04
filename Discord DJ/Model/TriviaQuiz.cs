

using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Discord_DJ.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            _audioConnection = await _channel.ConnectAsync();

            //TODO: Build player table

            PlaySnippet(_questions[0]);

            return TriviaServiceResult.Success;
        }

        private async Task PlaySnippet(TriviaItem item)
        {
            _questions.RemoveAt(0);
            _currentItem = item;
            _foundTitle = false;
            _foundSinger = false;

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
            bool consumed = false;

            if (!_foundSinger)
            {
                foreach (string singer in _currentItem.singer)
                {
                    if (answer.Content == singer)
                    {
                        consumed = true;
                        _foundSinger = true;
                        //TODO credit user points
                        break;
                    }
                }
            }

            if (!consumed && !_foundTitle)
            {
                foreach (string title in _currentItem.title)
                {
                    if (answer.Content == title)
                    {
                        consumed = true;
                        _foundTitle = true;
                        //TODO credit user points
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
                        if (answer.Content == title + ' ' + singer ||
                            answer.Content == singer + ' ' + title)
                        {
                            consumed = true;
                            _foundTitle = true;
                            _foundSinger = true;
                            //TODO credit user points;
                            break;
                        }
                    } 
                    if (consumed)
                    {
                        break;
                    }
                }
            }

            if (_foundSinger && _foundTitle)
            {
                await EndSnippet();
            }
        }       

        private async Task ShowQuestionCard(TriviaItem question)
        {
            //TODO: show embedded leaderboard with song title
        }

        private async Task ShowFinalLeaderboard()
        {
            //TODO: show embedded leaderboard with victor!
        }

        private async Task EndSnippet()
        {
            ShowQuestionCard(_currentItem);
            _streamCanceller.Cancel();
            _streamProcess.Dispose();
            _outputStream.Dispose();
            _discordStream.Dispose();

            if (_questions.Count > 0)
            {
                PlaySnippet(_questions[0]);
            }
            else
            {
                await ShowFinalLeaderboard();
                await _channel.DisconnectAsync();
                OnFinishedQuestions?.Invoke(this);
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
