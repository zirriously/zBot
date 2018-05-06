using System;
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
        private string _token = File.ReadAllText(@"..\..\token.txt");
        private string _clientId = File.ReadAllText(@"..\..\twitchclientid.txt");
        private List<string> _optOutList = File.ReadAllLines(@"..\..\users.txt").ToList();
        private const string _apiLink = "https://api.twitch.tv/kraken/streams/";
        private IRole liveRole;

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Info, MessageCacheSize = 100});
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();


            _client.GuildMemberUpdated += GuildMemberUpdated;
            _client.MessageReceived += MessageReceived;
            _client.Ready += () =>
            {
                int n = 0;
                foreach (var guild in _client.Guilds)
                {
                    n += guild.Users.Count;
                }
                Console.WriteLine($"{_client.CurrentUser.Username} is connected to {_client.Guilds.Count} guild, serving a total of {n} users. ");
                Console.WriteLine($"A total of {_optOutList.Count} are opted out.");

                foreach (var guild in _client.Guilds)
                {
                    foreach (var role in guild.Roles)
                    {
                        if (role.Name == "Live")
                            liveRole = role;
                    }
                }
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (after.Activity != null)
            {
                if (after.Activity.Type == ActivityType.Streaming && !_optOutList.Contains(after.Id.ToString()))
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
                        await after.AddRoleAsync(liveRole);
                        Console.WriteLine("Succesfully added role");
                        Console.ResetColor();
                    }
                }
            }
            else if (after.Roles.Contains(liveRole))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{after.Username} is no longer streaming. Removing role");
                await after.RemoveRoleAsync(liveRole);
                Console.WriteLine("Succesfully removed role");
                Console.ResetColor();
            }
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            if (msg.Channel.Name == "bot-stuff")
            {
                if (msg.Content.ToLower() == "..optout")
                {
                    if (!_optOutList.Contains(msg.Author.Id.ToString()))
                    {
                        _optOutList.Add(msg.Author.Id.ToString());
                        File.WriteAllLines(@"..\..\users.txt", _optOutList);
                        Console.WriteLine($"{msg.Author.Username} has opted out");
                        await msg.Channel.SendMessageAsync($"{msg.Author.Username} has opted out.");
                    }
                    else
                    {
                        Console.WriteLine($"{msg.Author.Username} already opted out");
                        await msg.Channel.SendMessageAsync($"Error - {msg.Author.Username} is already opted out.");
                    }
                }
                else if (msg.Content.ToLower() == "..optin")
                {
                    if (_optOutList.Contains(msg.Author.Id.ToString()))
                    {
                        _optOutList.Remove(msg.Author.Id.ToString());
                        File.WriteAllLines(@"..\..\users.txt", _optOutList);
                        Console.WriteLine($"{msg.Author.Username} has opted in");
                        await msg.Channel.SendMessageAsync($"{msg.Author.Username} has opted in.");
                    }
                    else
                    {
                        Console.WriteLine($"{msg.Author.Username} already opted in");
                        await msg.Channel.SendMessageAsync($"Error - {msg.Author.Username} is already opted in.");
                    }
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
