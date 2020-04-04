const { Command } = require('discord.js-commando');
const { MessageEmbed } = require('discord.js');
const Youtube = require('simple-youtube-api');
const ytdl = require('ytdl-core');
const { youtubeAPI } = require('../../config.json');
const youtube = new Youtube(youtubeAPI);

module.exports = class PlayCommand extends Command
{
    constructor(client)
    {
        super(client, 
            {
                name: 'play',
                memberName: 'play',
                group: 'music',
                description: 'Play any song or playlist from youtube',
                guildOnly: true,
                clientPermissions: ['SPEAK', 'CONNECT'],
                args: 
                [
                    {
                        key: 'query',
                        prompt: 'What song would you like to listen to?',
                        type: 'string',
                        validate: query => query.length > 0 && query.length < 200
                    }
                ]
            }
        );
    }

    async run(message, { query })
    {
        if (!Util.isInVoiceChannel(messag))
        {
            return; 
        } 

	if (query.match(/^(?!.*\?.*\bv=)https:\/\/www\.youtube\.com\/.*\?.*\blist=.*$/))
	{
	    try 
	    {
		const playlist = await youtube.getPlaylist(query);
		const videosObj = await playlist.getVideos();
		for (let i = 0 ; i < videosObj.length ; ++i)
		{
		    const video = await videosObj[i].fetch();

		    const url = `https://www.youtube.com/watch?v=${video.raw.id}`;
		    const title = video.raw.snippet.title;
		    let duration = this.formatDuration(video.duration);
		    const thumbnail = video.thumbnails.high.url;
		    if (duration == '00:00')
		    {
			duration = 'Live Stream';
		    }
		    const song = {
			url,
			title,
			duration,
			thumbnail,
			voiceChannel
		    };

		    message.guild.musicData.queue.push(song);
		}

		if (message.guild.musicData.isPlaying == false)
		{
		    message.guild.musicData.isPlaying = true;
		    return this.playSong(message.guild.musicData.queue, message);
		}
		else if (message.guild.musicData.isPlaying == true)
		{
		    return message.say(`Playlist - :musical_note:   ${playlist.title} :musical_note: has been added to the queue`);
		}
	    }
	    catch (err)
	    {
		console.error(err);
		return message.say('Playlist is either private or it does not exist');
	    }
	}
	else if (query.match(/^(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+/))
        {
            const url = query;
            try
            {
                query = query.replace(/(>|<)/gi, '').split(/(vi\/|v=|\/v\/|youtu\.be\/|\/embed\/)/);
                const id = query[2].split(/[^0-9a-z_\-]/i)[0]
                const video = await youtube.getVideoByID(id);
                const title = video.title;
                let duration = this.formatDuration(video.duration);
                const thumbnail = video.thumbnails.high.url;
                if (duration == '00:00')
                {
                    duration = 'Live Stream';
                }
                const song = 
                    {
                        url,
                        title,
                        duration,
                        thumbnail,
                        voiceChannel
                    };
                message.guild.musicData.queue.push(song);
                if (message.guild.musicData.isPlaying == false)
                {
                    message.guild.musicData.isPlaying = true;
                    return this.playSong(message.guild.musicData.queue, message);
                }
                else
                {
                    return message.say(`${song.title} added to queue`);
                }
            }
            catch (err)
            {
                console.error(err);
                return message.say('Something went wrong, please try again later');
            }
        }
	else
	{
	    try
            {
                const videos = await youtube.searchVideos(query, 5);
                if (videos.length < 5)
                {
                    return message.say('I had some trouble finding what you were looking for, please try again or be more specific');
                }
                const vidNameArr = [];
                for (let i = 0 ; i < videos.length ; ++i)
                {
                    vidNameArr.push(`${i + 1}: ${videos[i].title}`);
                }
                vidNameArr.push('exit');
                const embed = new MessageEmbed()
                    .setColor('#e9f931')
                    .setTitle('Choose a song by commenting a number between 1 and 5')
                    .addField('Song 1', vidNameArr[0])            
                    .addField('Song 2', vidNameArr[1])            
                    .addField('Song 3', vidNameArr[2])            
                    .addField('Song 4', vidNameArr[3])            
                    .addField('Song 5', vidNameArr[4])            
                    .addField('Exit', 'exit');            
                var songEmbed = await message.say({embed});
                try
                {
                    var response = await message.channel.awaitMessages(
                        msg => (msg.content > 0 && msg.content < 6) || msg.content === 'exit',
                        {
                            max: 1,
                            maxProcessed: 1,
                            time: 60000,
                            errors: ['time']
                        }
                    );
                    var videoIdx = parseInt(response.first().content);
                }
                catch (err)
                {
                    console.error(err);
                    songEmbed.delete();
                    return message.say('Please try again and enter a number between 1 and 5 or exit');
                }
                if (response.first().content === 'exit')
                {
                    return songEmbed.delete();
                }
                try
                {
                    var video = await youtube.getVideoByID(videos[videoIdx - 1].id);
                }
                catch (err)
                {
                    console.error(err);
                    songEmbed.delete();
                    return message.say('An error has occured when trying to get the video');
                }
                const url = `https://www.youtube.com/watch?v=${video.raw.id}`;
                const title = video.title;
                let duration = this.formatDuration(video.duration);
                const thumbnail = video.thumbnails.high.url;
                if (duration == '00:00')
                {
                    duration = 'Live Stream';
                }
                const song = 
                    {
                        url,
                        title,
                        duration,
                        thumbnail,
                        voiceChannel
                    };

                message.guild.musicData.queue.push(song);

                if (message.guild.musicData.isPlaying == false)
                {
                    message.guild.musicData.isPlaying = true;
                    return this.playSong(message.guild.musicData.queue, message);
                }
                else
                {
                    songEmbed.delete();
                    return message.say(`${song.title} added to the queue`);
                }
            }
            catch (err)
            {
                console.error(err);
                if (songEmbed)
                { 
                    songEmbed.delete();
                }
                return message.say('Something went wrong with searching the video you requested :(');
            }
	}
    }

    playSong(queue, message)
    {
       let voiceChannel;
       queue[0].voiceChannel.join().then(connection =>
           {
               const dispatcher = connection.play(ytdl(queue[0].url, {quality:'highestaudio',highWaterMark:1024*1024*10}))
                   .on('start', () =>
                   {
                       message.guild.musicData.songDispatcher = dispatcher;
                       dispatcher.setVolume(message.guild.musicData.volume);
                       voiceChannel = queue[0].voiceChannel;
                       const videoEmbed = new MessageEmbed()
                           .setThumbnail(queue[0].thumbnail)
                           .setColor('#e9f931')
                           .addField('Now Playing: ', queue[0].title)
                           .addField('Duration: ', queue[0].duration);
                       if (queue[1])
                       {
                           videoEmbed.addField('Next Song: ', queue[1].title);
                       }
                       message.say(videoEmbed);
                       return queue.shift();
                  }).on('finish', () =>
                  {
                      if (queue.length >= 1)
                      {
                          return this.playSong(queue, message);
                      }
                      else
                      {
                          message.guild.musicData.isPlaying = false;
                          return voiceChannel.leave();
                      }
                  }).on('error', () =>
                  {
                  });
            })
            .catch(e =>
            {
                console.error(e);
                return voiceChannel.leave();
            });
    }

    formatDuration(durationObj)
    {
        const duration = `${durationObj.hours ? durationObj.hours + ':' : ''}${durationObj.minutes ? durationObj.minutes : '00'}:${durationObj.seconds < 10 ? '0' + durationObj.seconds : durationObj.seconds ? durationObj.seconds : '00'}`;
        return duration;
    }
}