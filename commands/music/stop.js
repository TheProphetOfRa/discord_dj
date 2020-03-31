const { Command } = require('discord.js-commando');

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
        var voiceChannel = message.member.voice.channel;

        if (!voiceChannel)
        {
            return message.reply('Join a voice channel and try again');
        }

        if (typeof message.guild.musicData.songDispatcher == 'undefined' || message.guild.musicData.songDispatcher == null)
        {
            return message.reply('There is no song playing right now');
        }

        if (message.guild.musicData.queue.length > 0)
        {
            message.guild.musicData.queue.length = 0;
        }
        
        message.guild.musicData.songDispatcher.end();
    }
};
