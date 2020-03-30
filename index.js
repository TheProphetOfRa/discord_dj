const { CommandoClient } = require('discord.js-commando');
const { Structures } = require('discord.js');
const path = require('path');
const { prefix, token } = require('./config.json');

Structures.extend('Guild', Guild => 
{
    class MusicGuild extends Guild 
    {
        constructor(client, data) 
        {
            super(client, data);
            this.musicData = 
            {
                queue: [],
                isPlaying: false,
                volume: 1,
                songDispatcher: null
            };
        }
    }
    return MusicGuild;
});


const client = new CommandoClient(
{
    commandPrefix: prefix,
    owner: '210693416063991808',
    unknownCommandResponse: false
});

client.registry.registerDefaultTypes().registerGroups([['music', 'Music Command Group']]).registerDefaultGroups().registerDefaultCommands().registerCommandsIn(path.join(__dirname, 'commands')); 

client.once('ready', () =>
{
    console.log('Ready!');
});

client.login(token);
