const Discord = require('discord.js');
const {
    prefix,
    token
} = require('./config.json');
const ytdl = require('ytdl-core');

const queue = new Map();

const client = new Discord.Client();

client.once('ready', () => {
    console.log("Ready!");
});

client.once('reconnecting', () => {
    console.log("Reconnecting!");
});

client.once('disconnect', () => {
    console.log("Disconnect!");
});

client.on('error', error => {
    console.log(error);
});

client.on('message', async message => {
    console.log(`Received message: ${message}`);
    if (message.author.bot ||
	!message.content.startsWith(prefix))
    {
	return;
    }

    const serverQueue = queue.get(message.guild.id);
    if (message.content.startsWith(`${prefix} play`))
    {
	execute(message, serverQueue);
	return;
    }
    else if (message.startswith(`${prefix} skip`))
    {
	skip(message, serverQueue);
	return;
    } 
    else if (message.startswith(`${prefix} stop`))
    {
	stop(message, serverQueue);
	return;
    }
    else
    {
	message.channel.send('Invalid command.');
    }
});

async function execute(message, serverQueue)
{
    const args = message.content.split(' ');
    const voiceChannel = message.member.voiceChannel;
    if (!voiceChannel)
    {
	return message.channel.send('You need to be in a voice channel to play music!');
    }
    const permissions = voiceChannel.permissionsFor(message.client.user);
    if (!permissions.has('CONNECT') || !permissions.has('SPEAK'))
    {
	return message.channel.send('I need the permissions to join and speak in your voice channel.');
    }

    const songInfo = await ytdl.getInfo(args[2]);
    const song = {
	title: songInfo.title,
	url: songInfo.video_url
    };

    if (!serverQueue)
    {
	const queueContract = {
	    textChannel: message.channel,
	    voiceChannel: voiceChannel,
	    connection: null,
	    songs: [],
	    volume: 5,
	    playing: true
	};

	queue.set(message.guild.id, queueContract);
	queueContract.songs.push(song);

	try
	{
	    var connection = await voiceChannel.join();
	    queueContract.connection = connection;
	    play(message.guild, queueContract.songs[0]);
	}
	catch (err)
	{
	    console.log(err);
	    queue.delete(message.guild.id);
	    return message.channel.send(err);
	}
    }
    else
    {
	serverQueue.songs.push(song);
	console.log(serverQueue.songs);
	return message.channel.send(`${song.title} has been added to the queue!`);
    }
}

function play(guild, song)
{
    const serverQueue = queue.get(guild.id);
    if (!song)
    {
	serverQueue.voiceChannel.leave();
	queue.delete(guild.id);
	return;
    }

    let stream = ytdl(song.url, { filter: 'audioonly' });
    stream.on('end', () => 
    {
	console.log('Music ended.');
	serverQueue.songs.shift();
	play(guild, serverQueue.songs[0]);
    }).on('error', error => {
	console.log(error);
    });
    const dispatcher = serverQueue.connection.playStream(stream);

    //dispatcher.setVolumeLogarithmic(serverQueue.volume / 5);
}

function skip(message, serverQueue)
{
    if (!message.member.voiceChannel)
    {
	return message.channel.send('You have to be in a voice channel to stop the music!');
    }
    if (!serverQueue)
    {
	return message.channel.send('There is no song playing in this channel.');
    }
    serverQueue.connection.dispatcher.end();
}

function stop(message, serverQueue)
{
    console.log(`Stopping: ${serverQueue}`);
    if (!message.member.voiceChannel)
    {
	return message.channel.send('You have to be in a voice channel to skip a song.');
    }
    serverQueue.songs = [];
    serverQueue.connection.dispatcher.end();
}

client.login(token).then(console.log).catch(console.error);
