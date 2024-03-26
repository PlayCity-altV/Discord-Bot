namespace PlayCityDiscordBot.Config
{
    public record MainConfig(
        string Token, GuildInfo Guild, HostInfo Host, string LogPath, string RedeployWebhook
    );

    public record GuildInfo(string GuildId, string GuestRoleId, string QaRoleId, string SuggestionChannelId);
    public record HostInfo(string Host, string Username, string Password, int Port);
}