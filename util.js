module.exports = class Util
{
    isInVoiceChannel(message)
    {
        if (message.member.voice.channel)
        {
            message.reply('Join a channel and try again');
            return false;
        }

        return true; 
    }

    isPlayingMusic(message)
    {
        if (typeof message.guild.musicData.songDispatcher == 'undefined' || message.guild.musicData.songDispatcher == null)
        {
            message.reply('There is no song playing right now');
            return false;
        }

        return true;
    }
        
};
