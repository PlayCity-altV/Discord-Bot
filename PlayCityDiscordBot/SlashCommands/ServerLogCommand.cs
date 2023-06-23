using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using JetBrains.Annotations;
using PlayCityDiscordBot.Config;

namespace PlayCityDiscordBot.SlashCommands
{
    [DebugCommand]
    [UsedImplicitly]
    public class ServerLogCommand : ApplicationCommandModule
    {
        private readonly IConfig<MainConfig> _config;

        public ServerLogCommand(IConfig<MainConfig> config)
        {
            this._config = config;
        }

        [UsedImplicitly]
        [SlashCommand("getLog", "Получить лог сервера")]
        public async Task GetLog(InteractionContext ctx)
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
                using (var stream = new FileStream(this._config.Entries.LogPath, FileMode.Open))
                {
                    var messageBuilder = new DiscordMessageBuilder().AddFile(stream);
                    await ctx.Channel.SendMessageAsync(messageBuilder);
                }
                await ctx.DeleteResponseAsync();
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Невозможно получить лог"));
            }
        }
    }
}