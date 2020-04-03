using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord_DJ.Commands
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient m_client;
        private readonly CommandService m_commands;

        public CommandHandler(DiscordSocketClient i_client, CommandService i_commands)
        {
            m_commands = i_commands;
            m_client = i_client;
        }

        public async Task InstallCommandsAsync()
        {
            m_client.MessageReceived += HandleCommandAsync;

            await m_commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            int argPos = 0;

            if (!(message.HasCharPrefix('+', ref argPos) || message.HasMentionPrefix(m_client.CurrentUser, ref argPos)) || message.Author.IsBot)
            {
                return;
            }

            SocketCommandContext context = new SocketCommandContext(m_client, message);

            IResult result = await m_commands.ExecuteAsync(context: context, argPos: argPos, services: null);
        }
    }
}
