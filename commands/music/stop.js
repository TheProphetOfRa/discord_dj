const { Command } = require('discord.js-commando');
const { Util } = require.main.require(`${__base}util.js`);

module.exports = class StopCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'stop',
                aliases: ['skipall', 'skip-all', 'end'],
                memberName: 'stop',
                group: 'music',
                description: 'Empties the queue and ends the current song.',
                guildOnly: true
            }
        );
    }

    run(message)
    {
        if (Util.isInVoiceChannel(message) && Util.isPlayingMusic(message))
        {
            if (message.guild.musicData.queue.length > 0)
            {
                message.guild.musicData.queue.length = 0;
            }
            
            message.guild.musicData.songDispatcher.end();
        }
    }
};
