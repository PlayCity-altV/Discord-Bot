using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using PlayCityDiscordBot.Config;

namespace PlayCityDiscordBot.Services;

public interface IModalsService
{
    Task PostSuggestionMessage(DiscordClient sender, MessageCreateEventArgs e);
    Task SuggestionsButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs e);
    Task SuggestionsModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e);
}

public class ModalsService : IModalsService
{
    private readonly IConfig<MainConfig> _config;

    private Dictionary<ulong, DateTime> Cooldowns { get; set; } = new();

    public ModalsService(IConfig<MainConfig> config)
    {
        this._config = config;
    }

    public async Task PostSuggestionMessage(DiscordClient sender, MessageCreateEventArgs e)
    {
        // if (e.Message.Content.ToLower() != "create-suggestions-button".ToLower()) return; //todo
        if (e.Message.Channel.Id != 1121685169561161830 || e.Message.Author.Id != 310462724255514625) return;

        var guild = await sender.GetGuildAsync(ulong.Parse(this._config.Entries.Guild.GuildId));

        var guildMember = await guild.GetMemberAsync(e.Message.Author.Id);

        if ((guildMember.Permissions & Permissions.Administrator) != Permissions.Administrator) return;

        var channel = guild.GetChannel(e.Channel.Id);

        var buttonEN = new DiscordButtonComponent(ButtonStyle.Primary, "suggestions-button-en", "Create suggestion");
        var builderEN = new DiscordMessageBuilder().WithContent(
                "Click \"Create\" to contribute your ideas and influence our project. Your suggestions are valuable, and we carefully consider each one for possible implementation. Together we can turn your ideas into reality to make our project better.")
            .AddComponents(buttonEN);

        var buttonRU = new DiscordButtonComponent(ButtonStyle.Primary, "suggestions-button-ru", "Создать предложение");
        var builderRU = new DiscordMessageBuilder().WithContent(
                "Нажмите кнопку \"Создать\", чтобы внести свои идеи и повлиять на наш проект. Ваши предложения ценны, и мы внимательно рассматриваем каждое из них для возможной реализации. Вместе мы сможем превратить ваши идеи в реальность сделать наш проект лучше.")
            .AddComponents(buttonRU);

        await channel.SendMessageAsync(builderEN);
        await channel.SendMessageAsync(builderRU);
        await e.Message.DeleteAsync();
    }

    public async Task SuggestionsButtonPressed(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id != "suggestions-button-en" && e.Id != "suggestions-button-ru") return;
        bool english = e.Id == "suggestions-button-en";

        if (this.Cooldowns.TryGetValue(e.Interaction.User.Id, out var cooldown) &&
            cooldown > DateTime.UtcNow)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(english ? "You need to wait 2 minutes to create new suggestion" : "Вам необходимо подождать 2 минуты, чтобы создать еще 1 предложение")
                    .AsEphemeral());
            return;
        }

        this.Cooldowns.Remove(e.Interaction.User.Id);

        try
        {
            var response = new DiscordInteractionResponseBuilder()
                .WithTitle(english ? "Create suggestions" : "Создать предложение")
                .WithContent(english
                    ? "Your suggestions are valuable, and we carefully consider each one for possible implementation"
                    : "Ваши предложения ценны, и мы внимательно рассматриваем каждое из них для возможной реализации")
                .WithCustomId("modal-suggestion")
                .AddComponents(new TextInputComponent(english ? "Suggestion" : "Предложение", "suggestion-message",
                    english ? "Input your suggestion" : "Введите ваше предложение", required: true, min_length: 5));

            await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, response);
        }
        catch (BadRequestException exception)
        {
            Console.WriteLine(exception.JsonMessage);
            Console.WriteLine(exception.Errors);
        }
    }

    public async Task SuggestionsModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
    {
        try
        {
            if (e.Interaction.Data.CustomId != "modal-suggestion") return;

            var guild = await sender.GetGuildAsync(ulong.Parse(this._config.Entries.Guild.GuildId));
            var channel = guild.GetChannel(ulong.Parse(this._config.Entries.Guild.SuggestionChannelId));

            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Suggestion")
                .WithTitle($"From {e.Interaction.User.Username}")
                .WithDescription(e.Values.Values.ToList()[0]) // user suggestion
                .WithColor(new DiscordColor("#ff8500"))
                .WithThumbnail(
                    "https://media.discordapp.net/attachments/1105608698400882688/1105608728495018025/logo_no_bg.png");

            var message = await channel.SendMessageAsync(embed);
            var thread = await message.CreateThreadAsync("Discussion",
                AutoArchiveDuration.Week);

            await thread.SendMessageAsync(
                $"You can discuss suggestions in this thread");

            await message.CreateReactionAsync(DiscordEmoji.FromName(sender,":white_check_mark:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(sender,":x:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":who:")); //who emote;

            this.Cooldowns.Add(e.Interaction.User.Id, DateTime.UtcNow.AddMinutes(2));

            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        catch (BadRequestException exception)
        {
            Console.WriteLine(exception.JsonMessage);
            Console.WriteLine(exception.Errors);
        }
    }
}