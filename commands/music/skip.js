const { Command } = require('discord.js-commando');
const { Util } = require.main.require(`${__base}util.js`);

module.exports = class SkipCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'skip',
                aliases: ['skip-song', 'advance-song', 'no'],
                memberName: 'skip',
                group: 'music',
                description: 'Skip the currently playing song',
                guildOnly: true
            });
    }

    run(message)
    {
        if (Util.isInVoiceChannel(message) && Util.isPlayingMusic(message))
        {
            message.guild.musicData.songDispatcher.end();
        }
    }
};
