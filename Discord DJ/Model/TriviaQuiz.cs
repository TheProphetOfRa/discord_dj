

using Discord;
using Discord.Audio;
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
        private List<TriviaItem> _questions;

        public event OnFinishedQuestionsDelegate OnFinishedQuestions;

        private IAudioClient _audioConnection = null;
        private CancellationTokenSource _streamCanceller = null;
        private Process _streamProcess = null;
        private Stream _outputStream = null;
        private AudioOutStream _discordStream = null;

        public TriviaQuiz(ulong guildId, IVoiceChannel channel, List<TriviaItem> questions)
        {
            _guildId = guildId;
            _questions = questions;
            _channel = channel;
        }            

        public async Task<TriviaServiceResult> StartQuiz()
        {
            _audioConnection = await _channel.ConnectAsync();

            PlaySnippet(_questions[0]);

            return TriviaServiceResult.Success;
        }

        private async Task PlaySnippet(TriviaItem item)
        {
            _questions.RemoveAt(0);

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

        private async Task EndSnippet()
        {
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
