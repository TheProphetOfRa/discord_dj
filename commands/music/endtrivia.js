const { Command } = require('discord.js-commando');

module.exports = class EndTriviaCommand extends Command
{

    constructor(client)
    {  
        super(client, 
            {
                name: 'end-trivia',
                aliases: ['stop-trivia', 'stop-music-trivia', 'end-music-trivia'],
                memberName: 'end-trivia',
                group: 'music',
                description: 'End the trivia game.',
                guildOnly: true,
                clientPermissions: ['SPEAK', 'CONNECT']
            }
        );
    }

    run(message)
    {
        if (!message.guild.triviaData.isTriviaRunning)
        {
            return message.say('No trivia is currently running');
        }
        
        if (message.guild.me.voice.channel !== message.member.voice.channel)
        {
            return message.say("Join the trivia's channel and try again");
        }

        if (!message.guild.triviaData.triviaScore.has(message.author.username))
        {
            return message.say('You need to be a participant of the trivia to end it!');
        }

        message.guild.triviaData.triviaQueue.length = 0;
        message.guild.triviaData.wasTriviaEndCalled = true;
        message.guild.triviaData.triviaScore.clear();
        message.musicData.songDispatcher.end();
    }
};
