using System.Reflection;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PlayCityDiscordBot.Config;

namespace PlayCityDiscordBot
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class DebugCommandAttribute : Attribute
    {
    }

    public static class SlashCommandResolver
    {
        public static void ResolveCommands(this IServiceProvider serviceProvider, IConfig<MainConfig> config)
        {
            var client = serviceProvider.GetService<DiscordClient>();
            var slashCommands = client!.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = serviceProvider
            });

            slashCommands.SlashCommandErrored += (_, args) =>
            {
                Console.WriteLine($"Command {args.Context.CommandName} errored:\n{args.Exception}");
                return Task.CompletedTask;
            };

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                         .Where(e => e.IsClass && !e.IsAbstract && e.IsSubclassOf(typeof(ApplicationCommandModule))))
            {
                Console.WriteLine("Registered " + type.FullName);
                if (type.GetCustomAttribute<DebugCommandAttribute>() is null)
                    slashCommands.RegisterCommands(type);
                else
                    slashCommands.RegisterCommands(type, ulong.Parse(config.Entries.Guild.GuildId));
            }
        }
    }
}