using Discord_DJ.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord_DJ.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Discord_DJ
{
    public class Program
    {
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Config.LoadConfig();

            using (var services = ConfigureServices())
            {
                var m_client = services.GetRequiredService<DiscordSocketClient>();                            

                m_client.Log += Log;
                services.GetRequiredService<CommandService>().Log += Log;

                await m_client.LoginAsync(TokenType.Bot, Config.BotAPIKey);
                await m_client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitialiseAsync();

                //Block until program quits
                await Task.Delay(-1);
            }            
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<GoogleAPIService>()
                .AddSingleton<MusicService>()
                .BuildServiceProvider();
        }
    }
}
