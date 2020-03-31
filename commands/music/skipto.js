const { Command } = require('discord.js-commando');

module.exports = class SkipToCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'skipto',
                memberName: 'skipto',
                group: 'music',
                description: 'Skip to a specific song in the queue, provide the song number as an argument',
                guildOnly: true,
                args:
                [
                    {
                        key: 'songNumber',
                        prompt: 'What is the number in the queue you wish to skip to? For please use skip.',
                        type: 'integer'
                    }
                ]
            }
        );
    }
    
    run(message)
    {
        if (songNumber < 1 && songNumber >= message.guild.musicData.queue.length)
        {
            return message.reply('Please enter a valid song number');
        }

        var voiceChannel = message.member.voice.channel;

        if (!voiceChannel)
        {
            return message.reply('Join a channel and try again');
        }

        if (typeof message.guild.musicData.songDispatcher == 'undefined' || message.guild.musicData.songDispatcher == null)
        {
            return message.reply('There is no song playing right now');
        }

        if (message.guild.musicData.queue < 1)
        {
            return message.reply('There are no songs in the queue');
        }

        message.guild.musicData.queue.splice(0, songNumber-1);
        message.guild.musicData.songDispatcher.end();
    }
};
