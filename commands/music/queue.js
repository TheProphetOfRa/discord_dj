const { Command } = require('discord.js-commando');
const { MessageEmbed } = require('discord.js');

module.exports = class QueueCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'queue',
                aliases: ['song-list', 'next-songs'],
                group: 'music',
                memberName: 'queue',
                guildOnly: true,
                description: 'Display the song queue'
            }
        );
    }

    run(message)
    {
        if (message.guild.musicData.isTriviaRunning)
        {
            if (message.guild.me.voice.channel == message.member.voice.channel)
            {
                message.guild.triviaData.triviaScore.set(message.author.username, message.guild.triviaData.triviaScore.get(message.author.username - 1));
                return message.say("No cheating! -10 points :smiling_imp:");
            }
            else
            {
                return message.say("There is currently a quiz running, try again later.");
            }
        }
    
        if (message.guild.musicData.queue.length == 0)
        {
            return message.say('There are no songs in the queue, why not queue some up with the play command!');
        }

        const titleArray = [];

        message.guild.musicData.queue.map(obj =>
            {
                titleArray.push(obj.title);
            }
        );
        
        var queueEmbed = new MessageEmbed().setColor('#ff7373').setTitle('Queue');
        for (let i = 0 ; i < titleArray.length ; ++i)
        {
            queueEmbed.addField(`${i+1}: `, `${titleArray[i]}`);
        }

        return message.say(queueEmbed);
    }
};
