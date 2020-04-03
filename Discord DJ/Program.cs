using Discord_DJ.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Discord_DJ
{
    public class Program
    {
        private DiscordSocketClient m_client;
        private CommandService m_commandService;
        private CommandHandler m_commandHandler;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Config.LoadConfig();

            m_client = new DiscordSocketClient();

            m_commandService = new CommandService();
            m_commandHandler = new CommandHandler(m_client, m_commandService);

            await m_commandHandler.InstallCommandsAsync();

            m_client.Log += Log;

            //TODO: Load config json file with token instead of env variable
            await m_client.LoginAsync(TokenType.Bot, Config.BotAPIKey);
            await m_client.StartAsync();

            //Block until program quits
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
