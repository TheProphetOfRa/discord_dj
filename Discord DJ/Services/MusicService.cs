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
            MusicPlayer guildPlayer = null;

            if (!_mapGuildToMusicPlayer.ContainsKey(guildId))
            {
                guildPlayer = new MusicPlayer(guildId, _services);
                _mapGuildToMusicPlayer.Add(guildId, guildPlayer);
                guildPlayer.OnFinishedQueue += OnMusicPlayerFinished;
            }
            else
            {
                guildPlayer = _mapGuildToMusicPlayer[guildId];
            }

            return await guildPlayer.TryQueue(channel, musicRequest);
        }

        private void OnMusicPlayerFinished(MusicPlayer player)
        {
            _mapGuildToMusicPlayer.Remove(player.GuildId);
        }
    }
}
