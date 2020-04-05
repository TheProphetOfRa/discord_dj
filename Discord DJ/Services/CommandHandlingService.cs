using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord_DJ.Services
{
    class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        private readonly TriviaService _triviaService;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _triviaService = services.GetRequiredService<TriviaService>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitialiseAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            int argPos = 0;

            SocketCommandContext context = new SocketCommandContext(_discord, message);            

            if (!message.HasCharPrefix('+', ref argPos))
            {
                if (_triviaService.IsGuildRunningQuizInChannel(context.Guild.Id, message.Channel))
                {
                    await _triviaService.ProcessAnswer(context.Guild.Id, message);
                }
                return;
            }

            await _commands.ExecuteAsync(context: context, argPos: argPos, services: _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
            {
                return;
            }

            if (result.IsSuccess)
            {
                return;
            }

            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
