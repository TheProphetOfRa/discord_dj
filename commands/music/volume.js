const { Command } = require('discord.js-commando');
const { Util } = require.main.require(`${__base}util.js`);

module.exports = class VolumeCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'volume',
                aliases: ['change-volume'],
                group: 'music',
                memberName: 'volume',
                guildOnly: true,
                description: 'Adjust song volume',
                throttling:
                {
                    usages: 1,
                    duration: 5
                },
                args:
                [
                    {
                        key: 'wantedVolume',
                        prompt: 'What volume would you like to set? [1-200]',
                        type: 'integer',
                        validate: wantedVolume => wantedVolume >= 1 && wantedVolume <= 200
                    }
                ]
            }
        );
    }

    run(message, { wantedVolume })
    {
        if (Util.isInVoiceChannel(message) && Util.isPlayingMusic(message))
        {
            const volume = wantedVolume / 100;
            message.guild.musicData.volume = volume;
            message.guild.musicData.songDispatcher.setVolume(volume);
            message.say(`Current volume is set to ${wantedVolume}%`);
        }
    }
};
