using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlayCityDiscordBot.Config;

namespace PlayCityDiscordBot
{
    internal static class Program
    {
        private static void Main()
        {
            Run().GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            var config = new Config<MainConfig>();
            var discordConfig = new DiscordConfiguration
            {
                Token = config.Entries.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.Guilds | DiscordIntents.GuildMembers |
                          DiscordIntents.GuildIntegrations | DiscordIntents.GuildMessages |
                          DiscordIntents.GuildPresences | DiscordIntents.GuildMessageReactions |
                          DiscordIntents.DirectMessages
            };

            var discordClient = new DiscordClient(discordConfig);

            discordClient.ClientErrored += (_, args) =>
            {
                Console.WriteLine(args.Exception.ToString());
                return Task.CompletedTask;
            };
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton(discordClient)
                .AddSingleton(typeof(IConfig<>), typeof(Config<>))
                .BuildServiceProvider(true);

            var mainConfig = new Config<MainConfig>();
            
            serviceProvider.ResolveCommands(config);

            discordClient.GuildMemberAdded += (sender, args) =>
            {
                var guestRole = args.Guild.GetRole(ulong.Parse(mainConfig.Entries.Guild.GuestRoleId));
                args.Member.GrantRoleAsync(guestRole);
                
                return Task.CompletedTask;
            };

            await discordClient.ConnectAsync();
            
            Console.WriteLine("Play City Discord Bot Started");
            
            await Task.Delay(-1);
        }
    }
}