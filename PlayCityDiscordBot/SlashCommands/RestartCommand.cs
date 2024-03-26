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
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You don't have permissions"));
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
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Server restarted"));
                        client.RunCommand($"echo -e {this._config.Entries.Host.Password} | sudo -S docker restart core");
                        client.Disconnect();
                    }
                }
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Server didn't restarted"));
            }
        }
        
        [UsedImplicitly]
        [SlashCommand("redeploy", "Обновить сервер")]
        public async Task Redeploy(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());

            if (ctx.Guild.GetRole(ulong.Parse(this._config.Entries.Guild.QaRoleId)) is not { } role) return;
            
            if (ctx.Member.Hierarchy < role.Position)
            {
                var messageError =
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You don't have permissions"));
                await Task.Delay(10000);
                await ctx.DeleteFollowupAsync(messageError.Id);
            }

            try
            {
                if (this._config.Entries.RedeployWebhook is not { } webhook)
                {
                    var messageError =
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Redeploy webhook was not found"));
                    await Task.Delay(10000);
                    await ctx.DeleteFollowupAsync(messageError.Id);
                    return;
                }

                Console.WriteLine("Requesting service update...");
                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

                using var client = new HttpClient(handler);
                await client.PostAsync(webhook, null);
                Console.WriteLine("Request complete");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Server redeployed"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}