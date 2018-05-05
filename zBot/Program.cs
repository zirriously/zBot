using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Audio.Streams;

namespace zBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private string _token = File.ReadAllText(@"D:\Repos\Projects\DBot\DBot\token.txt");

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Info });
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            _client.Ready += () =>
            {
                int n = 0;
                foreach (var guild in _client.Guilds)
                {
                    n += guild.Users.Count;
                }
                Console.WriteLine($"Connected to {_client.Guilds.Count} guild, serving a total of {n} users.");
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}
