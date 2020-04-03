using Discord;
using Discord.Audio;
using Discord_DJ.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord_DJ.Model
{
    public class MusicPlayer
    {
        public delegate void OnFinishedQueueDelegate(MusicPlayer player);

        private readonly GoogleAPIService _googleAPIService;

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
        
        private List<string> _queuedUrls = new List<string>();

        public MusicPlayer(ulong guildId, IServiceProvider services)
        {
            _guildId = guildId;
            _googleAPIService = services.GetRequiredService<GoogleAPIService>();
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

            string videoUrl = null;

            if (!singleVideoDetector.IsMatch(musicRequest))
            {
                videoUrl = await _googleAPIService.TryGetVideoUrlForSearchTerms(musicRequest);
                if (string.IsNullOrEmpty(videoUrl))
                {
                    return MusicServiceResult.FailedToGetResults;
                }
            }
            else
            {
                videoUrl = musicRequest;
            }

            _queuedUrls.Add(videoUrl);

            if (!_isPlaying)
            {
                Play(_queuedUrls[0]);
            }

            return MusicServiceResult.Success;
        }

        private async Task ConnectToChannel()
        {
            _audioConnection = await _channel.ConnectAsync();
        }

        private async Task Play(string url)
        {
            _isPlaying = true;
            _queuedUrls.RemoveAt(0);

            if (_audioConnection == null || _audioConnection.ConnectionState == ConnectionState.Disconnected || _audioConnection.ConnectionState == ConnectionState.Disconnecting)
            {
                await ConnectToChannel();
            }

            using (var ffplay = CreateStream(url))
            using (var output = ffplay.StandardOutput.BaseStream)
            using (var discord = _audioConnection.CreatePCMStream(AudioApplication.Music))
            {
                try
                {
                    await output.CopyToAsync(discord);
                }
                finally
                {
                    await discord.FlushAsync();
                    if (_queuedUrls.Count > 0)
                    {
                        Play(_queuedUrls[0]);
                    }
                    else
                    {                        
                        await _channel.DisconnectAsync();
                        _isPlaying = false;
                        OnFinishedQueue(this);
                    }
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
