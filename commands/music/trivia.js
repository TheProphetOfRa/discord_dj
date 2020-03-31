const { Command }  = require('discord.js-commando');
const { MessageEmbed } = require('discord.js');
const ytdl = require('ytdl-core');
const fs = require('fs');

module.exports = class Trivia extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'trivia',
                memberName: 'trivia',
                aliases: ['music-trivia', 'quiz', 'start-quiz', 'music-quiz', 'start-trivia'],
                group: 'music',
                description: 'Engage in a music quiz with your friends!',
                guildOnly: true,
                clientPermissions: ['SPEAK', 'CONNECT'],
                throttling: 
                {
                    usages: 1,
                    duration: 10
                },
                args:
                [
                    {
                        key: 'numberOfSongs',
                        prompt: 'What is the number of songs you want to run?',
                        type: 'integer',
                        default: 5,
                        max: 15
                    }
                ]
            }
        );
    }

    async run(message, { numberOfSongs })
    {
        var voiceChannel = message.member.voice.channel;

        if (!voiceChannel)
        {
            return message.reply('Join a voice channel to start a quiz!');
        }

        if (message.guild.musicData.isPlaying === true)
        {
            return message.channel.send("A quiz or a song is already running. Please try again once it's finished");
        }

        message.guild.musicData.isPlaying = true;
        message.guild.triviaData.isTriviaRunning = true;

        const jsonSongs = fs.readFileSync(__dirname + '/../../resources/music/trivia.json', 'utf8');

        var videoDataArray = JSON.parse(jsonSongs).songs;
        const randomVideoLinks = this.getRandom(videoDataArray, numberOfSongs);
        const infoEmbed = new MessageEmbed().setColor('#ff7373').setTitle('Starting Music Quiz').setDescription(`Get ready! There are ${numberOfSongs} songs, you have 30 seconds to guess either the singer/band or the name of the song. Good Luck! You can end the quiz using the end-trivia command`);

        message.say(infoEmbed);

        for (let i = 0 ; i < randomVideoLinks.length ; ++i)
        {
            const song = 
                {
                    url: randomVideoLinks[i].url,
                    singer: randomVideoLinks[i].singer,
                    title: randomVideoLinks[i].title,
                    voiceChannel
                };
            message.guild.triviaData.triviaQueue.push(song);
        }

        const channelInfo = Array.from(message.member.voice.channel.members.entries());
        channelInfo.forEach(user =>
            {
                if (user[1].user.bot)
                {
                    return;
                }
                
                message.guild.triviaData.triviaScore.set(user[1].user.username, 0);
            }
        );

        this.playQuizSong(message.guild.triviaData.triviaQueue, message);
    }

    playQuizSong(queue, message)
    {
        queue[0].voiceChannel.join().then(connection => 
            {
                const dispatcher = connection.play(ytdl(queue[0].url, {quality: 'highestaudio', highWaterMark: 1024 * 1024 * 1024})).on('start', () =>
                    {
                        message.guild.musicData.songDispatcher = dispatcher;
                        dispatcher.setVolume(message.guild.musicData.volume);
                        let songNameFound = false;
                        let songSingerFound = false;

                        const filter = m => message.guild.triviaData.triviaScore.has(m.author.username);
                        const collector = message.channel.createMessageCollector(filter, 
                            {
                                time: 30000
                            }
                        );
                        
                        collector.on('collect', m => 
                            {
                                let neededTitle = queue[0].title.toLowerCase();
                                let neededSinger = queue[0].singer.toLowerCase();

                                if (!message.guild.triviaData.triviaScore.has(m.author.username))
                                {
                                    return;
                                }
                                if (m.content.startsWith(this.client.comandPrefix))
                                {
                                    return;
                                }
                                if (m.content.toLowerCase() === neededTitle)
                                {
                                    if (songNameFound)
                                    {
                                        return;
                                    }
                                    
                                    songNameFound = true;
                        
                                    if (songNameFound && songSingerFound)
                                    {
                                        collector.stop();
                                    }

                                    m.react('☑');
                                    message.guild.triviaData.triviaScore.set(m.author.username, message.guild.triviaData.triviaScore.get(m.author.username) + 1);
                                }
                                else if (m.content.toLowerCase() === neededSinger)
                                {
                                    if (songSingerFound)
                                    {
                                        return;
                                    }

                                    songSingerFound = true;
    
                                    if (songSingerFound && songNameFound)
                                    {
                                        collector.stop();
                                    }

                                    m.react('☑')
                                    message.guild.triviaData.triviaScore.set(m.author.username, message.guild.triviaData.triviaScore.get(m.author.username) + 1);
                                }
                                else if (m.content.toLowerCase() === neededSinger + ' ' + neededTitle || m.content.toLowerCase() === neededTitle + ' ' + neededSinger)
                                {
                                    if (!songSingerFound && !songNameFound)
                                    {
                                        message.guild.triviaData.triviaScore.set(m.author.username, message.guild.triviaData.triviaScore.get(m.author.username) + 1);
                                    } 
                                    
                                    message.guild.triviaData.triviaScore.set(m.author.username, message.guild.triviaData.triviaScore.get(m.author.username) + 1);
                                    m.react('☑');
                                    return collector.stop();
                                }
                                else
                                {
                                    return m.react('❌');
                                }
                            }
                        );

                        collector.on('end', () =>
                            {
                                if (message.guild.triviaData.wasTriviaCancelled)
                                {
                                    message.guild.triviaData.wasTriviaCancelled = false;
                                    return;
                                }

                                const sortedScoreMap = new Map([...message.guild.triviaData.triviaScore.entries()].sort((a, b) => b[1] - a[1]));

                                const song = `${this.capitalizeWords(queue[0].singer)}: ${this.capitalizeWords(queue[0].title)}`;
                                
                                const embed = new MessageEmbed().setColor('#ff7373').setTitle(`The song was: ${song}`).setDescription(this.getLeaderboard(Array.from(sortedScoreMap.entries())));

                                message.channel.send(embed);

                                queue.shift();
                                dispatcher.end();
                            }
                        );
                    }
                )
                .on('finish', () =>
                    {
                        if (queue.length >= 1)
                        {
                            return this.playQuizSong(queue, message);
                        }
                        else
                        {
                            if (message.guild.triviaData.wasTriviaEndCalled)
                            {
                                message.guild.musicData.isPlaying = false;
                                message.guild.triviaData.isTriviaRunning = false;
                                message.guild.me.voice.channel.leave();
                                return;
                            }
                            
                            const sortedScoreMap = new Map([...message.guild.triviaData.triviaScore.entries()].sort((a, b) => b[1] - a[1]));

                            const embed = new MessageEmbed().setColor('#ff7373').setTitle('Music Quiz Results').setDescription(this.getLeaderboard(Array.from(sortedScoreMap.entries())));

                            message.channel.send(embed);
                            message.guild.musicData.isPlaying = false;
                            message.guild.triviaData.isTriviaRunning = false;
                            message.guild.triviaData.triviaScore.clear();
                            message.guild.me.voice.channel.leave();
                        }
                    }
                );
            } 
        );
    }

    getRandom(arr, n)
    {  
        var result = new Array(n), len = arr.length, taken = new Array(len);

        if (n > len)
        {
            throw new RangeError('getRandom: more elements taken than available');
        }

        while (n--)
        {
            var x = Math.floor(Math.random() * len);
            result[n] = arr[x in taken ? taken[x] : x];
            taken[x] = --len in taken ? taken[len] : len;
        }

        return result;
    }

    getLeaderboard(arr)
    {
        if (!arr)
        {
            return;
        }
        
        let leaderboard = '';
        
        leaderboard = `👑   **${arr[0][0]}:** ${arr[0][1]}  points`;

        if (arr.length > 1)
        {
            for (let i = 0 ; i < arr.length ; ++i)
            {
                leaderboard = leaderboard + `\n\n   ${i + 1}: ${arr[i][0]}: ${arr[i][1]}  points`;
            }
        }

        return leaderboard;
    }

    capitalizeWords(str)
    {
        return str.replace(/\w\S*/g, function(txt)
                                        {
                                            return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
                                        });
    }
}
