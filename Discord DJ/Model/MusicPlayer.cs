using Discord;
using Discord.Audio;
using Discord_DJ.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_DJ.Model
{
    public class MusicPlayer
    {
        public class VideoInfo
        {
            public string title;
            public string url;
        };

        public delegate void OnFinishedQueueDelegate(MusicPlayer player);

        private readonly GoogleAPIService _googleAPIService;
        private readonly YoutubeDLService _youtubeDLService;

        public event OnFinishedQueueDelegate OnFinishedQueue;

        private IVoiceChannel _channel = null;
        public IVoiceChannel Channel 
        { 
            get
            {
                return _channel;
            }
        }

        private ulong _guildId;
        public ulong GuildId
        {
            get
            {
                return _guildId;
            }
        }

        private IAudioClient _audioConnection = null;

        private bool _isPlaying = false;
        
        private List<VideoInfo> _queue = new List<VideoInfo>();
        public List<VideoInfo> Queue
        {
            get
            {
                return _queue;
            }
        }

        Process _streamProcess = null;
        Stream _outputStream = null;
        AudioOutStream _discordStream = null;
        CancellationTokenSource _streamCanceller = null;


        public MusicPlayer(ulong guildId, IServiceProvider services)
        {
            _guildId = guildId;
            _googleAPIService = services.GetRequiredService<GoogleAPIService>();
            _youtubeDLService = services.GetRequiredService<YoutubeDLService>();
        }

        public async Task<MusicServiceResult> TryQueue(IVoiceChannel channel, string musicRequest)
        {
            if (_channel == null || _channel.Id != channel.Id)
            {
                if (_isPlaying)
                {
                    return MusicServiceResult.AlreadyPlayingInAnotherChannel;
                }
                else
                {
                    _channel = channel;
                }
            }

            Regex singleVideoDetector = new Regex(@"(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+");

            VideoInfo video = null;

            if (!singleVideoDetector.IsMatch(musicRequest))
            {
                video = await _googleAPIService.TryGetVideoForSearchTerms(musicRequest);
            }
            else
            {
                video = await _youtubeDLService.GetInfoForVideoUrl(musicRequest);
            }

            if (video == null)
            {
                return MusicServiceResult.FailedToGetResults;
            }

            _queue.Add(video);

            if (!_isPlaying)
            {
                Play(_queue[0]);
            }

            return MusicServiceResult.Success;
        }

        private async Task ConnectToChannel()
        {
            _audioConnection = await _channel.ConnectAsync();
        }

        private async Task Play(VideoInfo video)
        {
            _isPlaying = true;
            _queue.RemoveAt(0);

            if (_audioConnection == null || _audioConnection.ConnectionState == ConnectionState.Disconnected || _audioConnection.ConnectionState == ConnectionState.Disconnecting)
            {
                await ConnectToChannel();
            }

            _streamCanceller = new CancellationTokenSource();
            _streamProcess = CreateStream(video.url);
            _outputStream = _streamProcess.StandardOutput.BaseStream;
            _discordStream = _audioConnection.CreatePCMStream(AudioApplication.Music);

            try
            {
                await _outputStream.CopyToAsync(_discordStream, _streamCanceller.Token);
            }
            finally
            {
                await _discordStream.FlushAsync();
                _streamProcess.Dispose();
                _outputStream.Dispose();
                _discordStream.Dispose();
                if (_queue.Count > 0)
                {
                    Play(_queue[0]);
                }
                else
                {
                    await _channel.DisconnectAsync();
                    _isPlaying = false;
                    OnFinishedQueue(this);
                }
            }
        }

        public async Task<MusicServiceResult> Stop(IVoiceChannel channel)
        {
            if (_isPlaying && _channel.Id != channel.Id)
            {
                return MusicServiceResult.AlreadyPlayingInAnotherChannel;                
            }

            _queue.Clear();
            _streamCanceller.Cancel();

            return MusicServiceResult.Success;
        }

        public async Task<MusicServiceResult> Skip(IVoiceChannel channel)
        {
            if (_isPlaying && _channel.Id != channel.Id)
            {
                return MusicServiceResult.AlreadyPlayingInAnotherChannel;
            }

            //Cache this locally as the recursive Play function will set up a new one for the next track which will be associated with the member variable
            CancellationTokenSource canceller = _streamCanceller;
            canceller.Cancel();
            canceller.Dispose();
            return MusicServiceResult.Success;
        }

        public async Task<MusicServiceResult> SkipTo(IVoiceChannel channel, int songIndex)
        {
            if (_isPlaying && _channel.Id != channel.Id)
            {
                return MusicServiceResult.AlreadyPlayingInAnotherChannel;
            }

            _queue.RemoveRange(0, songIndex - 1);

            //Cache this locally as the recursive Play function will set up a new one for the next track which will be associated with the member variable
            CancellationTokenSource canceller = _streamCanceller;
            canceller.Cancel();
            canceller.Dispose();
            return MusicServiceResult.Success;
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
