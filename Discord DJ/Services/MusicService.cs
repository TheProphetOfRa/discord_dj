using Discord;
using Discord_DJ.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord_DJ.Services
{
    public enum MusicServiceResult
    {
        FailedToGetResults,
        AlreadyPlayingInAnotherChannel,
        Success
    }
    public class MusicService
    {
        private readonly IServiceProvider _services;

        private Dictionary<ulong, MusicPlayer> _mapGuildToMusicPlayer = new Dictionary<ulong, MusicPlayer>();
        
        public MusicService(IServiceProvider services)
        {
            _services = services;
        }

        public async Task<MusicServiceResult> TryAddToGuidQueue(ulong guildId, IVoiceChannel channel, string musicRequest)
        {
            if (!_mapGuildToMusicPlayer.ContainsKey(guildId))
            {
                _mapGuildToMusicPlayer.Add(guildId, new MusicPlayer(_services));
            }

            MusicPlayer guildPlayer = _mapGuildToMusicPlayer[guildId];
            return await guildPlayer.TryQueue(channel, musicRequest);
        }
    }
}
