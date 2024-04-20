namespace Vereesa.Neon.Tests.ServicesTests
{
    public class GuildApplicationServiceTests
    {
        // private DiscordSettings _discordSettings;
        // private GuildApplicationService _applicationService;
        // private DiscordSocketClient _discordClient;

        // [TestInitialize]
        // public void Before()
        // {
        //     //Set up configuration
        //     var builder = new ConfigurationBuilder()
        //         .SetBasePath(AppContext.BaseDirectory)
        //         .AddJsonFile("config.Test.json", optional: false, reloadOnChange: true)
        //         .AddJsonFile("config.Test.Local.json", optional: true, reloadOnChange: true);

        //     var config = builder.Build();
        //     _battleNetApiSettings = new BattleNetApiSettings();
        //     config.GetSection(nameof(BattleNetApiSettings)).Bind(_battleNetApiSettings);

        //     _applicationService = new GuildApplicationService();
        //     _discordClient = new DiscordSocketClient();

        //     _discordClient.LoginAsync(TokenType.Bot, _discordSettings.Token);
        //     _discordClient.StartAsync();
        // }

        // [Fact]
        // public void SendApplicationEmbed_ApplicationEmbedGenerating_ApplicationEmbedSent()
        // {
        //     var discord = new DiscordSocketClient();
        //     var discordSettings = new DiscordSettings();

        //     var discordToken = _settings.Token;

        //     if (string.IsNullOrWhiteSpace(discordToken))
        //         throw new Exception("Please enter your bot's token into the `config.json` file found in the applications root directory.");

        //     await _discord.LoginAsync(TokenType.Bot, discordToken);
        //     await _discord.StartAsync();

        // }
    }
}
