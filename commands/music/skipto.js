const { Command } = require('discord.js-commando');
const { Util } = require.main.require(`${__base}util.js`);

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

        if (!Util.isInVoiceChannel(message) || !Util.isPlayingMusic(message))
        {
            return;
        }

        if (message.guild.musicData.queue < 1)
        {
            return message.reply('There are no songs in the queue');
        }

        message.guild.musicData.queue.splice(0, songNumber-1);
        message.guild.musicData.songDispatcher.end();
    }
};
