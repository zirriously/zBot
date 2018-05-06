﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Audio.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace zBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private string _token = File.ReadAllText(@"D:\Repos\Projects\zBot\zBot\token.txt");
        private string _clientId = File.ReadAllText(@"D:\Repos\Projects\zBot\zBot\twitchclientid.txt");
        private const string _apiLink = "https://api.twitch.tv/kraken/streams/";

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Info, MessageCacheSize = 100});
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();


            _client.GuildMemberUpdated += GuildMemberUpdated;
            _client.Ready += () =>
            {
                int n = 0;
                foreach (var guild in _client.Guilds)
                {
                    n += guild.Users.Count;
                }
                Console.WriteLine($"{_client.CurrentUser.Username} is connected to {_client.Guilds.Count} guild, serving a total of {n} users. ");
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (after.Activity.Type == ActivityType.Streaming)
            {
                StreamingGame streamingGame = (StreamingGame) after.Activity;
                string twitchUsername = streamingGame.Url.Substring(streamingGame.Url.LastIndexOf('/') + 1);
                string apiReq = _apiLink + twitchUsername;
                string response = await TwitchRequest(apiReq);

                dynamic dResp = JsonConvert.DeserializeObject<dynamic>(response);
                string game = dResp.stream.game;
                Console.WriteLine($"{after.Username} is now streaming {game}");

                if (game == "Factorio")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Giving role to {after.Username}");
                    Console.ResetColor();
                }
            }
        }

        private Task<string> TwitchRequest(string url)
        {
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
            req.Method = "Get";
            req.Headers.Add("Client-ID", _clientId);

            HttpWebResponse webResponse = (HttpWebResponse) req.GetResponse();
            StreamReader sr = new StreamReader(webResponse.GetResponseStream());
            string strResponse = sr.ReadToEnd();

            return Task.FromResult(strResponse);
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}
