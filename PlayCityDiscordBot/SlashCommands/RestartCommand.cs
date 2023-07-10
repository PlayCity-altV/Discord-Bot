using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using JetBrains.Annotations;
using PlayCityDiscordBot.Config;
using Renci.SshNet;

namespace PlayCityDiscordBot.SlashCommands
{
    [DebugCommand]
    [UsedImplicitly]
    public class RestartCommand : ApplicationCommandModule
    {
        private readonly IConfig<MainConfig> _config;

        public RestartCommand(IConfig<MainConfig> config)
        {
            this._config = config;
        }

        [UsedImplicitly]
        [SlashCommand("restart", "Перезагрузить сервер")]
        public async Task Restart(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());

            if (ctx.Guild.GetRole(ulong.Parse(this._config.Entries.Guild.QaRoleId)) is not { } role) return;

            if (ctx.Member.Hierarchy < role.Position)
            {
                var messageError =
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("У вас недостаточно прав"));
                await Task.Delay(10000);
                await ctx.DeleteFollowupAsync(messageError.Id);
            }

            try
            {
                using (var client = new SshClient(this._config.Entries.Host.Host, this._config.Entries.Host.Port,
                           this._config.Entries.Host.Username, this._config.Entries.Host.Password))
                {
                    client.Connect();

                    if (client.IsConnected)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Сервер перезагружен"));
                        client.RunCommand($"echo -e {this._config.Entries.Host.Password} | sudo -S docker restart core");
                        client.Disconnect();
                    }
                }
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Сервер не был перезагружен"));
            }
        }
    }
}