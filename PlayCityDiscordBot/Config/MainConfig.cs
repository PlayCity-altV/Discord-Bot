namespace PlayCityDiscordBot.Config
{
    public record MainConfig(
        string Token, GuildInfo Guild, HostInfo Host
    );

    public record GuildInfo(string GuildId, string GuestRoleId, string QaRoleId);
    public record HostInfo(string Host, string Username, string Password, int Port);
}